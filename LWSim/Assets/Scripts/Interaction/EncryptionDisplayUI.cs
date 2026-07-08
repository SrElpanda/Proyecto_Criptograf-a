using UnityEngine;
using System.Linq;

public class EncryptionDisplayUI : MonoBehaviour
{
    [Header("References")]
    public CentralHub hub;

    private SelectionManager selectionManager;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private GUIStyle clickableStyle;
    private bool stylesInitialized;
    private int? expandedBatchId;
    private Vector2 scrollPosition;

    void Start()
    {
        if (hub == null)
            hub = FindAnyObjectByType<CentralHub>();

        selectionManager = FindAnyObjectByType<SelectionManager>();
    }

    void OnGUI()
    {
        if (hub == null || selectionManager == null)
            return;

        if (selectionManager.selectedObject == null)
            return;

        CentralHub selectedHub =
            selectionManager.selectedObject.GetComponent<CentralHub>();

        if (selectedHub == null)
            return;

        InitStyles();

        float panelWidth = (Screen.width / 2f) - 20f;
        float panelHeight = 350f;
        float margin = 10f;

        Rect panelRect = new Rect(
            margin,
            Screen.height - panelHeight - margin,
            panelWidth,
            panelHeight
        );

        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.Box(panelRect, "");
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(
            panelRect.x + 15,
            panelRect.y + 10,
            panelWidth - 30,
            panelHeight - 20
        ));

        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition,
            GUI.skin.horizontalScrollbar,
            GUI.skin.verticalScrollbar
        );

        string modeLabel = hub.useEncryption
            ? "ENCRYPTION: ON (Ascon-128 AEAD)"
            : "ENCRYPTION: OFF";

        string pauseLabel = SensorNode.SendingPaused
            ? "  |  <color=yellow>PAUSED</color>"
            : "";

        GUILayout.Label(
            $"[ HUB ] Total messages: {hub.messageHistory.Count}  |  {modeLabel}{pauseLabel}",
            titleStyle);
        GUILayout.Space(5);

        var lastBatches = hub.batches
            .TakeLast(5)
            .Reverse()
            .ToList();

        if (lastBatches.Count == 0)
        {
            GUILayout.Label("Waiting for messages...", labelStyle);
        }
        else
        {
            foreach (var batch in lastBatches)
            {
                bool isExpanded = expandedBatchId == batch.batchId;
                string prefix = isExpanded ? "\u25BC" : "\u25B8";
                string matchIcon = batch.allMatch
                    ? "<color=#00FF00>\u2713</color>"
                    : "<color=#FF0000>\u2717</color>";

                string sensors =
                    string.Join(", ",
                        batch.sensorIds
                            .Select(s => s.Replace("sensor_", ""))
                            .Take(3));

                if (batch.sensorIds.Count > 3)
                    sensors += $" ... +{batch.sensorIds.Count - 3} more";

                string headerText =
                    $"{prefix} Batch #{batch.batchId}:  {batch.messageCount} msgs  " +
                    $"from [{sensors}]  |  {matchIcon}";

                if (GUILayout.Button(headerText, clickableStyle))
                {
                    expandedBatchId = isExpanded ? null : batch.batchId;
                }

                if (isExpanded)
                {
                    GUILayout.Space(2);

                    if (batch.entries != null && batch.entries.Count > 0)
                    {
                        foreach (var entry in batch.entries.Take(10))
                        {
                            string matchColor = entry.match ? "#00FF00" : "#FF0000";
                            string matchChar = entry.match ? "\u2713" : "\u2717";
                            GUILayout.Label(
                                $"  <color={matchColor}>{matchChar}</color> " +
                                $"[{Truncate(entry.timestamp, 19)}] " +
                                $"{entry.sensorId.Replace("sensor_", "")}  " +
                                $"{entry.decryptedJson}",
                                labelStyle
                            );
                        }

                        if (batch.entries.Count > 10)
                        {
                            GUILayout.Label(
                                $"  ... and {batch.entries.Count - 10} more messages",
                                labelStyle
                            );
                        }
                    }

                    GUILayout.Space(2);

                    if (hub.useEncryption && batch.encryptedSamples.Count > 0)
                    {
                        GUILayout.Label(
                            $"  Encrypted:  {batch.encryptedSamples[0]}",
                            labelStyle
                        );
                    }

                    GUILayout.Label(
                        $"  Decrypted:  {batch.decryptedSample}",
                        labelStyle
                    );

                    GUILayout.Space(5);
                }
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "(empty)";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    void InitStyles()
    {
        if (stylesInitialized)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            richText = true
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            richText = true,
            fontStyle = FontStyle.Normal
        };

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            richText = true
        };

        clickableStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        clickableStyle.hover.textColor = Color.yellow;
        clickableStyle.active.textColor = Color.white;

        stylesInitialized = true;
    }
}
