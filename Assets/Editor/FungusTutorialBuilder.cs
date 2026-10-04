using Fungus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FungusTutorialBuilder
{
    private const string ScenePath = "Assets/Scenes/Tutorial.unity";

    [MenuItem("Tools/Build Fungus Tutorial Flowchart Directly")]
    public static void BuildFlowchartDirectly()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Flowchart flowchart = Object.FindFirstObjectByType<Flowchart>();
        if (flowchart == null)
        {
            flowchart = new GameObject("Flowchart", typeof(Flowchart)).GetComponent<Flowchart>();
            Undo.RegisterCreatedObjectUndo(flowchart.gameObject, "Create Tutorial Flowchart");
        }

        ClearBlocks(flowchart);

        GameObject tutorialManager = GameObject.Find("Game Manager");
        Block intro = CreateBlock(flowchart, "Block_Intro", new Vector2(100f, 100f));
        AddEvent<GameStarted>(intro);
        AddSendMessage(intro, "Tutorial_Intro");

        Block dialogueIntro = CreateBlock(flowchart, "Dialogue_Intro", new Vector2(350f, 100f));
        AddMessageEvent(dialogueIntro, "Tutorial_Intro");
        AddSay(dialogueIntro, "Selamat datang, Petugas. Kamu bertugas memeriksa pengajuan produk Nutri-Level.");
        AddSay(dialogueIntro, "Periksa nama produk, produsen, kadar gula, garam, dan lemak sebelum mengambil keputusan.");
        AddSay(dialogueIntro, "Pelanggan pertama akan datang. Baca pengajuannya, lalu seret stempel yang sesuai ke area cap.");
        if (tutorialManager != null) AddCallMethod(dialogueIntro, tutorialManager, "SpawnCustomer1");

        Block customerTwo = CreateBlock(flowchart, "Wait_Cap_1_Done", new Vector2(100f, 280f));
        AddMessageEvent(customerTwo, "Tutorial_Customer2_Intro");
        AddSay(customerTwo, "Bagus! Cap pertama berhasil diberikan. Sekarang pelanggan kedua datang.");
        AddSay(customerTwo, "Baca data pengajuannya. Jika ada hal mencurigakan, gunakan tombol Investigasi.");
        if (tutorialManager != null) AddCallMethod(customerTwo, tutorialManager, "SpawnCustomer2");

        Block reject = CreateBlock(flowchart, "Wait_Investigate_Done", new Vector2(350f, 280f));
        AddMessageEvent(reject, "Tutorial_Reject_Instruction");
        AddSay(reject, "Investigasi selesai. Data asli sudah terbuka dan produk ini terbukti bermasalah.");
        AddSay(reject, "Produk berbahaya tidak boleh diberi cap. Tekan tombol TOLAK untuk menolak pengajuan.");

        Block finish = CreateBlock(flowchart, "Wait_Reject_Done", new Vector2(600f, 280f));
        AddMessageEvent(finish, "Tutorial_Finished");
        AddSay(finish, "Kerja bagus! Pelanggan pergi dan tutorial selesai. Kamu siap bertugas di kantor utama.");
        AddLoadScene(finish, "Main Dev");

        EditorUtility.SetDirty(flowchart);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Tutorial] Flowchart Fungus berhasil dibuat di Tutorial.unity.");
    }

    private static void ClearBlocks(Flowchart flowchart)
    {
        foreach (Block block in flowchart.GetComponents<Block>())
        {
            foreach (Command command in block.CommandList)
            {
                if (command != null) Undo.DestroyObjectImmediate(command);
            }

            EventHandler eventHandler = GetObjectReference<EventHandler>(block, "eventHandler");
            if (eventHandler != null) Undo.DestroyObjectImmediate(eventHandler);
            Undo.DestroyObjectImmediate(block);
        }
    }

    private static Block CreateBlock(Flowchart flowchart, string name, Vector2 position)
    {
        Block block = flowchart.CreateBlock(position);
        block.BlockName = name;
        Undo.RegisterCreatedObjectUndo(block, "Create " + name);
        return block;
    }

    private static T AddEvent<T>(Block block) where T : EventHandler
    {
        T eventHandler = Undo.AddComponent<T>(block.gameObject);
        eventHandler.ParentBlock = block;
        SetObjectReference(block, "eventHandler", eventHandler);
        return eventHandler;
    }

    private static void AddMessageEvent(Block block, string message)
    {
        MessageReceived eventHandler = AddEvent<MessageReceived>(block);
        SetString(eventHandler, "message", message);
    }

    private static T AddCommand<T>(Block block) where T : Command
    {
        T command = Undo.AddComponent<T>(block.gameObject);
        command.ParentBlock = block;
        block.CommandList.Add(command);
        EditorUtility.SetDirty(block);
        return command;
    }

    private static void AddSay(Block block, string text)
    {
        Say say = AddCommand<Say>(block);
        SetString(say, "storyText", text);
    }

    private static void AddCallMethod(Block block, GameObject target, string methodName)
    {
        CallMethod call = AddCommand<CallMethod>(block);
        SetObjectReference(call, "targetObject", target);
        SetString(call, "methodName", methodName);
    }

    private static void AddSendMessage(Block block, string message)
    {
        Fungus.SendMessage send = AddCommand<Fungus.SendMessage>(block);
        SetEnum(send, "messageTarget", (int)MessageTarget.SameFlowchart);
        SetStringData(send, "_message", message);
    }

    private static void AddLoadScene(Block block, string sceneName)
    {
        LoadScene load = AddCommand<LoadScene>(block);
        SetStringData(load, "_sceneName", sceneName);
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new MissingReferenceException(propertyName + " tidak ditemukan pada " + target.GetType().Name);
        property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringData(Object target, string propertyName, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        SerializedProperty stringValue = property?.FindPropertyRelative("stringVal");
        if (stringValue == null) throw new MissingReferenceException(propertyName + ".stringVal tidak ditemukan pada " + target.GetType().Name);
        stringValue.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new MissingReferenceException(propertyName + " tidak ditemukan pada " + target.GetType().Name);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new MissingReferenceException(propertyName + " tidak ditemukan pada " + target.GetType().Name);
        property.enumValueIndex = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetObjectReference<T>(Object target, string propertyName) where T : Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        return property?.objectReferenceValue as T;
    }
}
