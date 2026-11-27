using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public static class ProceduralUIBuilder
{
    static readonly Color dark = new Color32(24,24,33,255);
    static readonly Color panel = new Color32(39,39,49,255);
    static readonly Color pink = new Color32(240,65,155,255);
    static DefaultControls.Resources resources = new DefaultControls.Resources();
    [MenuItem("Tools/Audio Procedural/Crear interfaz de prueba")]
    static void Build() {
        if(GameObject.Find("ProceduralCanvas_Test") != null) { EditorUtility.DisplayDialog("Interfaz", "Ya existe ProceduralCanvas_Test. Elimínalo o renómbralo para crear otro.", "OK"); return; }
        var root = new GameObject("ProceduralCanvas_Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root,"Crear interfaz procedural");
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1000,650); scaler.matchWidthOrHeight = .5f;
        if(Object.FindFirstObjectByType<EventSystem>() == null) {
            var es = new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); Undo.RegisterCreatedObjectUndo(es,"Crear EventSystem");
        }
        var bg = Box(root.transform,"Fondo",dark,0,0,1,1);
        Label(bg,"AUDIO PROCEDURAL",.05f,.87f,.72f,.98f,32);
        var export = MakeButton(bg,"EXPORT",.76f,.89f,.95f,.97f);
        var top = Box(bg,"Original",panel,.05f,.64f,.95f,.86f);
        var import = MakeButton(top,"IMPORT FILE",.03f,.57f,.32f,.94f);
        var fileName = Label(top,"Sin archivo",.35f,.57f,.97f,.94f,18);
        var dd = DefaultControls.CreateDropdown(resources); Place(dd,top,.03f,.12f,.65f,.56f);
        var dropdown = dd.GetComponent<Dropdown>(); dropdown.ClearOptions(); dropdown.AddOptions(new List<string>{"Selecciona instrumento","Guitarra","Bajo","Batería"});
        var original = MakeButton(top,"PLAY ORIGINAL",.69f,.12f,.97f,.56f);
        var mid = Box(bg,"Acompañamientos",panel,.05f,.29f,.95f,.61f);
        Label(mid,"Selecciona uno o dos acompañamientos",.03f,.72f,.97f,.98f,22);
        string[] names={"GUITARRA","BAJO","BATERÍA"}; var toggles=new Toggle[3];
        for(int i=0;i<3;i++) {
            var tile=Box(mid,names[i],dark,.03f+i*.32f,.30f,.33f+i*.32f,.70f);
            var go=DefaultControls.CreateToggle(resources); Place(go,tile,.08f,.15f,.94f,.85f);
            var t=go.GetComponent<Toggle>(); t.isOn=false; t.group=null;
            var text=go.GetComponentInChildren<Text>(); text.text=names[i]; text.color=Color.white; text.fontSize=18;
            toggles[i]=t; go.AddComponent<ProceduralToggleVisual>().Configure();
        }
        var generate=MakeButton(mid,"GENERATE",.03f,.05f,.97f,.24f);
        var bottom=Box(bg,"Resultado",panel,.05f,.04f,.95f,.26f);
        var status=Label(bottom,"Esperando selección",.03f,.65f,.97f,.98f,20);
        var generated=MakeButton(bottom,"Acompañamiento",.03f,.15f,.34f,.60f);
        var all=MakeButton(bottom,"Reproducir todo",.36f,.15f,.72f,.60f);
        var stop=MakeButton(bottom,"Detener",.74f,.15f,.97f,.60f);
        var controller=root.AddComponent<ProceduralPreviewController>(); controller.original=dropdown; controller.instruments=toggles; controller.generate=generate; controller.playOriginal=original; controller.playGenerated=generated; controller.playAll=all; controller.stop=stop; controller.status=status; controller.importFile=import; controller.fileName=fileName; controller.reader=Object.FindFirstObjectByType<MusicXMLReader>(); controller.playback=Object.FindFirstObjectByType<PlaybackManager>();
        var modal = Box(bg,"ExportPanel",new Color32(12,12,18,245),0,0,1,1);
        var dialog = Box(modal,"Dialog",panel,.18f,.13f,.82f,.87f);
        Label(dialog,"EXPORT MUSIC — WAV",.05f,.84f,.95f,.98f,28);
        Label(dialog,"Nombre del archivo",.06f,.73f,.94f,.83f,18);
        var input = DefaultControls.CreateInputField(resources); input.name="FileNameInput"; Place(input,dialog,.06f,.61f,.94f,.73f);
        var nameInput=input.GetComponent<InputField>(); nameInput.text="Mi_composicion";
        Label(dialog,"Selecciona las pistas a exportar",.06f,.50f,.94f,.60f,18);
        var modeGo=DefaultControls.CreateDropdown(resources); modeGo.name="ExportMode"; Place(modeGo,dialog,.06f,.37f,.94f,.49f);
        var mode=modeGo.GetComponent<Dropdown>(); mode.ClearOptions(); mode.AddOptions(new List<string>{"Acompañamientos juntos","Acompañamientos separados","Base + acompañamientos juntos","Todas las pistas separadas"}); mode.value=2; mode.RefreshShownValue();
        var exportStatus=Label(dialog,"Formato: WAV",.06f,.20f,.94f,.35f,18);
        var confirm=MakeButton(dialog,"EXPORTAR",.06f,.05f,.48f,.18f);
        var cancel=MakeButton(dialog,"CANCELAR",.52f,.05f,.94f,.18f);
        controller.exportButton=export; controller.exportPanel=modal.gameObject; controller.exportMode=mode; controller.exportName=nameInput; controller.exportStatus=exportStatus; controller.confirmExport=confirm; controller.cancelExport=cancel;
        modal.gameObject.SetActive(false); export.interactable=false;
        EditorUtility.SetDirty(controller); EditorSceneManager.MarkSceneDirty(root.scene); Selection.activeGameObject=root;
    }
    static void Place(GameObject go,Transform parent,float x,float y,float r,float t) {
        go.transform.SetParent(parent,false); var rect=go.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
    static Transform Box(Transform parent,string name,Color color,float x,float y,float r,float t) {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); Place(go,parent,x,y,r,t); go.GetComponent<Image>().color=color; return go.transform;
    }
    static Text Label(Transform parent,string value,float x,float y,float r,float t,int size) {
        var go=DefaultControls.CreateText(resources); Place(go,parent,x,y,r,t); var text=go.GetComponent<Text>(); text.text=value; text.color=Color.white; text.fontSize=size; text.alignment=TextAnchor.MiddleCenter; return text;
    }
    static Button MakeButton(Transform parent,string value,float x,float y,float r,float t) {
        var go=DefaultControls.CreateButton(resources); go.name=value; Place(go,parent,x,y,r,t); go.GetComponent<Image>().color=pink; var text=go.GetComponentInChildren<Text>(); text.text=value; text.fontSize=20; text.color=Color.black; return go.GetComponent<Button>();
    }
}
