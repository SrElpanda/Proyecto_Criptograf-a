using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SelectionInfoUI : MonoBehaviour
{
    public CentralHub hub;

    private SelectionManager selectionManager;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;

    void Start()
    {
        selectionManager = GetComponent<SelectionManager>();
    }

    void OnGUI()
    {
        if (selectionManager == null)
            return;

        if (selectionManager.currentMode != SelectionManager.SelectMode.Inspect)
            return;

        if (selectionManager.selectedObject == null)
            return;

        SensorNode node =
            selectionManager.selectedObject.GetComponent<SensorNode>();

        if (node == null)
            return;

        InitStyles();

        float panelWidth = 300f;
        float panelHeight = 400f;
        float margin = 10f;

        Rect panelRect = new Rect(
            Screen.width - panelWidth - margin,
            margin,
            panelWidth,
            panelHeight
        );

        GUI.Box(panelRect, "");

        GUILayout.BeginArea(new Rect(
            panelRect.x + 10,
            panelRect.y + 10,
            panelWidth - 20,
            panelHeight - 20
        ));

        GUILayout.Label("SENSOR DETAILS", titleStyle);
        GUILayout.Space(5);

        GUILayout.Label($"ID: {node.SensorId}", labelStyle);
        GUILayout.Label(
            $"Temp: {node.lastTemperature:F1}°C",
            labelStyle
        );

        Vector3 pos = selectionManager.selectedObject.transform.position;
        GUILayout.Label(
            $"Position: ({pos.x:F1}, {pos.z:F1})",
            labelStyle
        );

        GUILayout.Space(10);
        GUILayout.Label("LAST MESSAGES", headerStyle);
        GUILayout.Space(5);

        if (hub != null)
        {
            List<MessagePacket> packets = hub.receivedPackets
                .Where(p =>
                    p.message != null &&
                    p.message.sensorId == node.SensorId)
                .TakeLast(5)
                .Reverse()
                .ToList();

            if (packets.Count == 0)
            {
                GUILayout.Label("No messages yet", labelStyle);
            }
            else
            {
                foreach (var packet in packets)
                {
                    GUILayout.Label(
                        $"[{packet.message.timestamp}] " +
                        $"{packet.message.temperature:F1}°C " +
                        $"(seq #{packet.sequenceNumber})",
                        labelStyle
                    );
                }
            }
        }

        GUILayout.EndArea();
    }

    void InitStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12
        };
    }
}
