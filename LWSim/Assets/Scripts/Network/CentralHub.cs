using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CentralHub : MonoBehaviour
{
    [Header("Settings")]
    public bool useEncryption = false;
    public float batchInterval = 3f;

    public List<MessagePacket> receivedPackets =
        new List<MessagePacket>();

    public List<MessageHistoryEntry> messageHistory =
        new List<MessageHistoryEntry>();

    public List<MessageBatch> batches =
        new List<MessageBatch>();

    private IMessageStrategy strategy;
    private PlainStrategy plainStrategy;
    private AsconStrategy asconStrategy;

    private List<MessageHistoryEntry> pendingBatch =
        new List<MessageHistoryEntry>();
    private float batchTimer;
    private int batchCounter;

    [System.Serializable]
    public class MessageHistoryEntry
    {
        public string sensorId;
        public string originalJson;
        public string encryptedData;
        public string decryptedJson;
        public bool match;
        public string timestamp;
    }

    [System.Serializable]
    public class MessageBatch
    {
        public int batchId;
        public string startTime;
        public string endTime;
        public int messageCount;
        public List<string> sensorIds;
        public List<string> encryptedSamples;
        public string decryptedSample;
        public bool allMatch;
        public List<MessageHistoryEntry> entries;
    }

    void Awake()
    {
        plainStrategy = new PlainStrategy();
        asconStrategy = new AsconStrategy();
        strategy = plainStrategy;
        batchTimer = batchInterval;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            SensorNode.SendingPaused = !SensorNode.SendingPaused;

        batchTimer -= Time.deltaTime;
        if (batchTimer <= 0f && pendingBatch.Count > 0)
        {
            FinalizeBatch();
            batchTimer = batchInterval;
        }
    }

    public void SetStrategy(IMessageStrategy newStrategy)
    {
        strategy = newStrategy;
    }

    public void ReceivePacket(MessagePacket packet)
    {
        receivedPackets.Add(packet);

        SensorMessage msg = packet.Unwrap(strategy);

        string originalJson = plainStrategy.Serialize(msg);

        string encrypted;
        string decryptedJson;
        bool match;

        if (useEncryption)
        {
            encrypted = asconStrategy.Serialize(msg);
            SensorMessage decrypted = asconStrategy.Deserialize(encrypted);
            decryptedJson = plainStrategy.Serialize(decrypted);
            match = originalJson == decryptedJson;
        }
        else
        {
            encrypted = "(encryption disabled)";
            decryptedJson = originalJson;
            match = true;
        }

        var entry = new MessageHistoryEntry
        {
            sensorId = msg.sensorId,
            originalJson = originalJson,
            encryptedData = encrypted,
            decryptedJson = decryptedJson,
            match = match,
            timestamp = msg.timestamp
        };

        messageHistory.Add(entry);
        pendingBatch.Add(entry);
    }

    void FinalizeBatch()
    {
        if (pendingBatch.Count == 0)
            return;

        var batch = new MessageBatch
        {
            batchId = batchCounter++,
            startTime = pendingBatch.First().timestamp,
            endTime = pendingBatch.Last().timestamp,
            messageCount = pendingBatch.Count,
            sensorIds = pendingBatch
                .Select(e => e.sensorId)
                .Distinct()
                .ToList(),
            encryptedSamples = pendingBatch
                .Where(e => e.encryptedData != "(encryption disabled)")
                .Take(3)
                .Select(e => e.encryptedData)
                .ToList(),
            decryptedSample = pendingBatch
                .First().decryptedJson,
            allMatch = pendingBatch.All(e => e.match),
            entries = new List<MessageHistoryEntry>(pendingBatch)
        };

        batches.Add(batch);

        Debug.Log(
            $"[Hub] Batch #{batch.batchId}: {batch.messageCount} msgs " +
            $"from {batch.sensorIds.Count} sensors, " +
            $"all match: {batch.allMatch}"
        );

        pendingBatch.Clear();
    }
}
