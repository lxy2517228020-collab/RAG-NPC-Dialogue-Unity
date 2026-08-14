using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RAGNPCDialogue.Editor
{
    public static class Phase2SceneSetup
    {
        private const string LevelScenePath = "Assets/Scenes/Level.unity";
        private const string RolandDialoguePath = "Assets/RAGNPCDialogue/Generated/DialogueSets/Roland_NormalConversation.asset";

        [MenuItem("Tools/AI Tools/Setup Phase 2 Sample Scene")]
        public static void SetupLevelScene()
        {
            Scene scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

            DialoguePresenter presenter = EnsureDialogueUI();
            EnsureRoland(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Phase 2 sample scene setup complete.");
        }

        private static DialoguePresenter EnsureDialogueUI()
        {
            DialoguePresenter existingPresenter = Object.FindAnyObjectByType<DialoguePresenter>();
            if (existingPresenter != null)
            {
                return existingPresenter;
            }

            GameObject canvasObject = new GameObject("DialogueCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("DialoguePanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.04f);
            panelRect.anchorMax = new Vector2(0.92f, 0.28f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            TMP_Text speakerText = CreateText("SpeakerNameText", panelObject.transform, 24, FontStyles.Bold);
            RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0.04f, 0.68f);
            speakerRect.anchorMax = new Vector2(0.96f, 0.92f);
            speakerRect.offsetMin = Vector2.zero;
            speakerRect.offsetMax = Vector2.zero;

            TMP_Text dialogueText = CreateText("DialogueText", panelObject.transform, 22, FontStyles.Normal);
            RectTransform dialogueRect = dialogueText.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0.04f, 0.22f);
            dialogueRect.anchorMax = new Vector2(0.96f, 0.68f);
            dialogueRect.offsetMin = Vector2.zero;
            dialogueRect.offsetMax = Vector2.zero;

            TMP_Text continueText = CreateText("ContinueText", panelObject.transform, 16, FontStyles.Italic);
            RectTransform continueRect = continueText.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.04f, 0.04f);
            continueRect.anchorMax = new Vector2(0.96f, 0.2f);
            continueRect.offsetMin = Vector2.zero;
            continueRect.offsetMax = Vector2.zero;
            continueText.alignment = TextAlignmentOptions.Right;
            continueText.text = "Press E to continue";

            DialoguePresenter presenter = canvasObject.AddComponent<DialoguePresenter>();
            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("dialoguePanel").objectReferenceValue = panelObject;
            serializedPresenter.FindProperty("speakerNameText").objectReferenceValue = speakerText;
            serializedPresenter.FindProperty("dialogueText").objectReferenceValue = dialogueText;
            serializedPresenter.FindProperty("continueText").objectReferenceValue = continueText;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            panelObject.SetActive(false);
            return presenter;
        }

        private static TMP_Text CreateText(string name, Transform parent, int fontSize, FontStyles style)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = name;
            return text;
        }

        private static void EnsureRoland(DialoguePresenter presenter)
        {
            GameObject roland = GameObject.Find("Roland");
            if (roland == null)
            {
                roland = new GameObject("Roland");
                roland.transform.position = FindPlayerStartPosition() + new Vector3(2f, 0f, 0f);

                SpriteRenderer spriteRenderer = roland.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/Circle.png");
                spriteRenderer.color = new Color(0.25f, 0.65f, 1f, 1f);
                spriteRenderer.sortingOrder = 5;

                BoxCollider2D collider = roland.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(2.2f, 2.2f);
            }

            NPCDialogueSource source = roland.GetComponent<NPCDialogueSource>();
            if (source == null)
            {
                source = roland.AddComponent<NPCDialogueSource>();
            }

            DialogueTrigger2D trigger = roland.GetComponent<DialogueTrigger2D>();
            if (trigger == null)
            {
                trigger = roland.AddComponent<DialogueTrigger2D>();
            }

            SerializedObject serializedSource = new SerializedObject(source);
            serializedSource.FindProperty("dialogueSet").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueSet>(RolandDialoguePath);
            serializedSource.FindProperty("npcDisplayName").stringValue = "Roland";
            serializedSource.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("dialogueSource").objectReferenceValue = source;
            serializedTrigger.FindProperty("dialoguePresenter").objectReferenceValue = presenter;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 FindPlayerStartPosition()
        {
            GameObject player = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
            return player != null ? player.transform.position : Vector3.zero;
        }
    }
}
