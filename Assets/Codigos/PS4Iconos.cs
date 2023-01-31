using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

public class PS4Iconos : MonoBehaviour {

    [DllImport("universal")]
    private static extern int FreeUnjail(int FWVersion);

    [DllImport("universal")]
    private static extern int FreeMount();

    enum update_ret
    {
        UPDATE_FOUND,
        UPDATE_ERROR,
        NO_UPDATE
    }

    [DllImport("universal")]
    private static extern update_ret sceStoreApiCheckUpdate(string title_id);

    [DllImport("universal")]
    private static extern bool sceStoreApiLaunchStore(string title_id);

    [DllImport("universal")]
    private static extern void Load_Store_API_Preqs_BigApp();




    public static PS4Iconos instancia;


    public GameObject FadeInFondo;
    public Sprite ControlesES;
    public Text txtMascara;
    public Text txtDebug;
    public Text txtMesaje;
    public GameObject Controles;
    public GameObject ControlesPasoPaso;
    public GameObject PanelManual;

    public GameObject PanelIconos;
    public Image[] Iconos;

    public Image IconoPreview;
    
    public Image AplicarJuego;
    public Image AplicarMascara;
    public Image AplicarCapa;

    public Sprite[] MascaraPNG;
    public Sprite[] CapasPNG;
    private int IdMask = 0;

    public Toggle chkAplicarSistema;
    public Toggle chkCarpetaTransparente;
    public Toggle chkPasoPaso;
    public GameObject botonAplicar;
    public GameObject botonManual;
    public GameObject botonCheckUpdate;

    public Text txtAplicar;
    public Text txtSalir;

    private bool chkAplicarSistemaActual;
    private bool chkCarpetaTransparenteActual;

    private string[] scaneo;
    private string[] scaneoSis;
    private string[] scaneoExt;

    int Posicion = 0;
    private bool Paso = true;
    private bool AplicarPasoPaso = false;
    private List<string> todosIconos = new List<string>();
    string AplicandoMascara = "Apply mask to: ";
                
    private bool MascaraOn = true;
    private bool ConCapa = false;

    private bool EstoyAplicando = true;
    public bool FalloRW = false;
    public bool Espanol = false;

    private int ReescalarTamaño = 0;
    private int ResscalarOffsetX = 0;
    private int ResscalarOffsetY = 0;

    public bool HuboCambios = false;
    
    public void Awake()
    {
        instancia = this;
    }
    
	void Start ()
    {
        try
        {
            FreeUnjail(0);
            FreeMount();
        }
        catch { ;}



        StartCoroutine(SuperPermiso());
        EscanearApps();

        if (Application.systemLanguage == SystemLanguage.Spanish)
        {
            Espanol = true;

            Controles.GetComponent<Image>().sprite = ControlesES;
            chkAplicarSistema.GetComponentInChildren<Text>().text = "Aplicar a Apps del Sistema";
            chkCarpetaTransparente.GetComponentInChildren<Text>().text = "Icono de Carpetas transparente";
            chkPasoPaso.GetComponentInChildren<Text>().text = "Paso a paso";
            botonAplicar.GetComponentInChildren<Text>().text = "Aplicar";
            botonManual.GetComponentInChildren<Text>().text = "Iconos\nPre-hechos";
            botonCheckUpdate.GetComponentInChildren<Text>().text = "Chequer\nUpdate";
            txtMascara.text = "Máscara: PS5";
            AplicandoMascara = "Aplicar máscara a: ";

            txtAplicar.text = "Aplicar";
            txtSalir.text = "Salir";
        }
    }

    IEnumerator SuperPermiso()
    {
        yield return new WaitForSeconds(0.5f);

        scaneoSis = Directory.GetFileSystemEntries("/system_ex/app/");
        
        // comprobar que realmente hay permisos de R/W
        yield return null;
        try
        {
            if (File.Exists(scaneoSis[0] + "/sce_sys/param.sfo"))
            {
                File.Copy(scaneoSis[0] + "/sce_sys/param.sfo", scaneoSis[0] + "/sce_sys/param.bak", true);
                File.Delete(scaneoSis[0] + "/sce_sys/param.bak");

                if (Espanol)
                {
                    txtMesaje.text = "Permisos R/W obtenidos en las carpetas del sistema ;-)";
                }
                else
                {
                    txtMesaje.text = "R/W permissions obtained on systems folders ;-)";
                }
                chkAplicarSistema.isOn = true;
            }
        }
        catch
        {
            chkAplicarSistema.interactable = false;
            chkCarpetaTransparente.interactable = false;
            FalloRW = true;
            if (Espanol)
            {
                txtMesaje.text = "No se pudo obtener permisos de R/W, no se podrá aplicar máscaras a los íconos de las apps del sistema... pruebe reiniciar la aplicación";
            }
            else
            {
                txtMesaje.text = "Could not get R/W permisions, can't apply masks to system apps icons... try restart the app";
            }
            txtMesaje.color = Color.red;

            Iconos[1].color = new Color(1f, 1f, 1f, 0.3f);
            Iconos[2].color = new Color(1f, 1f, 1f, 0.3f);
        }

        FadeInFondo.GetComponent<Animator>().enabled = true;
        Controles.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(botonAplicar);
        EstoyAplicando = false;

        // para el paso a paso
        if (!FalloRW)
        {
            foreach (string juegoSis in scaneoSis)
            {
                if (File.Exists(juegoSis + "/sce_sys/icon0.png"))
                {
                    todosIconos.Add(juegoSis);
                }
            }
        }
    }

    private void EscanearApps()
    {
        // cargar Iconos
        scaneo = Directory.GetFileSystemEntries("/user/appmeta/");
        if (Directory.Exists("/user/appmeta/external/"))
        {
            scaneoExt = Directory.GetFileSystemEntries("/user/appmeta/external/");
        }

        foreach (string juego in scaneo)
        {
            if (!File.Exists(juego + "/icon0.dds") && File.Exists(juego + "/icon0.png"))
            {
                if (!File.Exists(juego + "/icon0.png.bak")) // crear backup xq no hay DDS
                {
                    File.Copy(juego + "/icon0.png", juego + "/icon0.png.bak");
                }
            }

            if (File.Exists(juego + "/icon0.png"))
            {
                todosIconos.Add(juego); // para el paso a paso
            }
        }

        // crear .bak para juegos en disco externo
        if (scaneoExt != null)
        {
            foreach (string juegoEx in scaneoExt)
            {
                try
                {
                    if (!File.Exists(juegoEx + "/icon0.dds") && File.Exists(juegoEx + "/icon0.png"))
                    {
                        if (!File.Exists(juegoEx + "/icon0.png.bak")) // crear backup
                        {
                            File.Copy(juegoEx + "/icon0.png", juegoEx + "/icon0.png.bak");
                        }
                    }
                }
                catch { ;}

                if (File.Exists(juegoEx + "/icon0.png"))
                {
                    todosIconos.Add(juegoEx); // para el paso a paso
                }
            }
        }
    }

    public void Update()
    {
        if (!EstoyAplicando)
        {
            if (Input.GetKeyDown(KeyCode.Joystick1Button4) || Input.GetKeyDown(KeyCode.Q))
            {
                IdMask--;
                CambiarMascara();
            }
            if (Input.GetKeyDown(KeyCode.Joystick1Button5) || Input.GetKeyDown(KeyCode.E))
            {
                IdMask++;
                CambiarMascara();
            }
        }
        else
        {
            if (chkPasoPaso.isOn)
            {
                // moverse entre Iconos en el Paso a Paso
                if ((Input.GetAxis("dpad1_horizontal") > 0 || Input.GetKey(KeyCode.D)) && Posicion < todosIconos.Count - 1 && Paso)
                {
                    Paso = false;
                    Posicion++;

                    if (todosIconos[Posicion].IndexOf("/system_ex") == 0)
                    {
                        IconoPreview.sprite = LeerImagenPNG(todosIconos[Posicion] + "/sce_sys/icon0.png", 512);
                    }
                    else
                    {
                        IconoPreview.sprite = LeerImagenPNG(todosIconos[Posicion] + "/icon0.png", 512);
                    }

                    txtDebug.text = AplicandoMascara + todosIconos[Posicion].Substring(todosIconos[Posicion].Length - 9, 9);
                    StartCoroutine(SeguirPasando());
                }
                if ((Input.GetAxis("dpad1_horizontal") < 0 || Input.GetKey(KeyCode.A)) && Posicion > 0 && Paso)
                {
                    Paso = false;
                    Posicion--;

                    if (todosIconos[Posicion].IndexOf("/system_ex") == 0)
                    {
                        IconoPreview.sprite = LeerImagenPNG(todosIconos[Posicion] + "/sce_sys/icon0.png", 512);
                    }
                    else
                    {
                        IconoPreview.sprite = LeerImagenPNG(todosIconos[Posicion] + "/icon0.png", 512);
                    }
                    txtDebug.text = AplicandoMascara + todosIconos[Posicion].Substring(todosIconos[Posicion].Length - 9, 9);
                    StartCoroutine(SeguirPasando());
                }

                if ((Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKey(KeyCode.X)) && AplicarPasoPaso)
                {
                    StartCoroutine(AplicandoPasoPaso(MascaraOn, todosIconos[Posicion]));
                }

                if (Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKey(KeyCode.B))
                {
                    AplicarPasoPaso = false;
                    PanelIconos.SetActive(true);
                    ControlesPasoPaso.SetActive(false);
                    EstoyAplicando = false;

                    if (HuboCambios)
                    {
                        if (Espanol)
                        {
                            txtDebug.text = "La máscara fue aplicada, reinicia tu PS4 para que veas los cambios\no cambia de máscara (L1 o R1) y aplica de nuevo";
                        }
                        else
                        {
                            txtDebug.text = "The mask was applied, reboot the PS4 to see the changes\nor change the mask (L1 or R1) and apply again";
                        }
                    }
                    else
                    {
                        CambiarMascara();
                    }
                }
            }
        }

        if (PanelManual.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.B))
            {
                PanelManual.SetActive(false);
                EstoyAplicando = false;

                if (HuboCambios)
                {
                    if (Espanol)
                    {
                        txtDebug.text = "Los íconos fueron reemplazados, reinicia tu PS4 para que veas los cambios\no cambia de máscara (L1 o R1) y aplica de nuevo";
                    }
                    else
                    {
                        txtDebug.text = "The icons were replaced, reboot the PS4 to see the changes\nor change the mask (L1 or R1) and apply again";
                    }
                }
                else
                {
                    CambiarMascara();
                }
            }
        }
    }

    IEnumerator SeguirPasando()
    {
        yield return new WaitForSeconds(0.2f);
        Paso = true;
    }

    public void togleIconosSistema()
    {
        ActualizarIconos();
    }

    public void togleCarpetaTransparente()
    {
        ActualizarIconos();
    }

    public void toglePasoPaso()
    {
        chkAplicarSistema.interactable = !chkPasoPaso.isOn;
        chkCarpetaTransparente.interactable = !chkPasoPaso.isOn;

        if (chkPasoPaso.isOn)
        {
            chkAplicarSistemaActual = chkAplicarSistema.isOn;
            chkCarpetaTransparenteActual = chkCarpetaTransparente.isOn;

            chkAplicarSistema.isOn = false;
            chkCarpetaTransparente.isOn = false;
        }
        else
        {
            chkAplicarSistema.isOn = chkAplicarSistemaActual;
            chkCarpetaTransparente.isOn = chkCarpetaTransparenteActual;
        }
    }

    public void BotonClick()
    {
        AplicarPasoPaso = false;
        Controles.SetActive(false);
        
        if (chkPasoPaso.isOn)
        {
            PanelIconos.SetActive(false);
            ControlesPasoPaso.SetActive(true);
            
            EstoyAplicando = true;
            HuboCambios = false;

            Posicion = 0;
            IconoPreview.sprite = LeerImagenPNG(todosIconos[Posicion] + "/icon0.png", 512);
            txtDebug.text = AplicandoMascara + todosIconos[Posicion].Substring(todosIconos[Posicion].Length - 9, 9);

            StartCoroutine(SeguirAplicandoPasoPaso());
        }
        else
        {
            EstoyAplicando = true;
            StartCoroutine(Aplicando(MascaraOn));
        }
    }

    IEnumerator SeguirAplicandoPasoPaso()
    {
        yield return new WaitForSeconds(0.5f);
        AplicarPasoPaso = true;
    }

    public void BotonManualClick()
    {
        HuboCambios = false;

        Controles.SetActive(false);
        EstoyAplicando = true;
        PanelManual.SetActive(true);
    }

    public void BotonCheckUpdate()
    {
        try
        {
            Load_Store_API_Preqs_BigApp();

            if (sceStoreApiCheckUpdate("LAPY20007") == update_ret.UPDATE_FOUND)
            {
                if (!sceStoreApiLaunchStore("LAPY20007"))
                {
                    if (Espanol)
                    {
                        txtMascara.text = "Error, no se puede actualizar la app...";
                    }
                    else
                    {
                        txtMascara.text = "Error, can not update the app...";
                    }
                }
            }
            else
            {
                if (Espanol)
                {
                    txtMascara.text = "No se encuentró ninguna actualización...";
                }
                else
                {
                    txtMascara.text = "No update found...";
                }
            }
        }
        catch { ;}
    }

    public void LateUpdate()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (ControlesPasoPaso.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(ControlesPasoPaso.transform.GetChild(0).gameObject); 
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(botonAplicar);
            }
        }
    }

    private void CambiarMascara()
    {
        txtDebug.text = "";
        MascaraOn = true;
        ConCapa = false;
        AplicarCapa.sprite = null;
        ReescalarTamaño = 0;
        ResscalarOffsetX = 0;
        ResscalarOffsetY = 0;

        if (!Controles.activeInHierarchy)
        {
            Controles.SetActive(true);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(botonAplicar);    
        }

        if (IdMask < 0) IdMask = MascaraPNG.Length;
        else if (IdMask >= MascaraPNG.Length + 1) IdMask = 0;

        string NombreMascara = "Mask Name: ";
        if (Espanol)
        {
            NombreMascara = "Máscara: ";
        }

        switch (IdMask)
        {
            case 0:
                txtMascara.text = NombreMascara + "PS5";
                break;
            case 1:
                txtMascara.text = NombreMascara + "PS5 [White Border]";
                break;
            case 2:
                txtMascara.text = NombreMascara + "Circle";
                break;
            case 3:
                txtMascara.text = NombreMascara + "Circle [White Border]";
                break;
            case 4:
                txtMascara.text = NombreMascara + "PS4 Disk";
                break;
            case 5:
                txtMascara.text = NombreMascara + "PS4 Box";
                ReescalarTamaño = 453;
                ResscalarOffsetX = 24;
                ResscalarOffsetY = 61;
                break;
            case 6:
                txtMascara.text = NombreMascara + "Anubis [Square]";
                ReescalarTamaño = 466;
                ResscalarOffsetX = 22;
                ResscalarOffsetY = 25;
                AplicarCapa.sprite = CapasPNG[0];
                ConCapa = true;
                break;
            case 7:
                txtMascara.text = NombreMascara + "Anubis [Polygon]";
                ReescalarTamaño = 492;
                ResscalarOffsetX = 11;
                ResscalarOffsetY = 7;
                AplicarCapa.sprite = CapasPNG[1];
                ConCapa = true;
                break;
            case 8:
                txtMascara.text = NombreMascara + "Anubis [Bubbles]";
                ReescalarTamaño = 476;
                ResscalarOffsetX = 18;
                ResscalarOffsetY = 18;
                AplicarCapa.sprite = CapasPNG[2];
                ConCapa = true;
                break;
            case 9:
                txtMascara.text = NombreMascara + "Anubis [Glass Box]";
                ReescalarTamaño = 460;
                ResscalarOffsetX = 45;
                ResscalarOffsetY = 20;
                AplicarCapa.sprite = CapasPNG[3];
                ConCapa = true;
                break;
            case 10:
                txtMascara.text = NombreMascara + "Anubis [Mechanic]";
                ReescalarTamaño = 416;
                ResscalarOffsetX = 45;
                ResscalarOffsetY = 47;
                AplicarCapa.sprite = CapasPNG[4];
                ConCapa = true;
                break;
            case 11:
                txtMascara.text = NombreMascara + "Nano [Ring]";
                ReescalarTamaño = 441;
                ResscalarOffsetX = 39;
                ResscalarOffsetY = 29;
                AplicarCapa.sprite = CapasPNG[5];
                ConCapa = true;
                break;
            case 12:
                txtMascara.text = NombreMascara + "Old Movie";
                break;
            case 13:
                txtMascara.text = NombreMascara + "Vita HEX [PSVita]";
                ReescalarTamaño = 331;
                ResscalarOffsetX = 91;
                ResscalarOffsetY = 112;
                AplicarCapa.sprite = CapasPNG[6];
                ConCapa = true;
                break;
            case 14:
                txtMascara.text = NombreMascara + "Vita HEX [PS1]";
                ReescalarTamaño = 504;
                ResscalarOffsetX = 43;
                ResscalarOffsetY = 5;
                break;
            case 15:
                txtMascara.text = NombreMascara + "Anubis [Cat]";
                ReescalarTamaño = 380;
                ResscalarOffsetX = 64;
                ResscalarOffsetY = 64;
                AplicarCapa.sprite = CapasPNG[7];
                ConCapa = true;
                break;
            case 16:
                txtMascara.text = NombreMascara + "Anubis [Futurix]";
                ReescalarTamaño = 442;
                ResscalarOffsetX = 36;
                ResscalarOffsetY = 27;
                AplicarCapa.sprite = CapasPNG[8];
                ConCapa = true;
                break;
            case 17:
                txtMascara.text = NombreMascara + "Anubis [i9]";
                ReescalarTamaño = 410;
                ResscalarOffsetX = 45;
                ResscalarOffsetY = 43;
                AplicarCapa.sprite = CapasPNG[9];
                ConCapa = true;
                break;
            case 18:
                txtMascara.text = NombreMascara + "Anubis [NVidia]";
                ReescalarTamaño = 487;
                ResscalarOffsetX = 14;
                ResscalarOffsetY = 0;
                AplicarCapa.sprite = CapasPNG[10];
                ConCapa = true;
                break;
            case 19:
                txtMascara.text = NombreMascara + "Anubis [OneUI]";
                ReescalarTamaño = 486;
                ResscalarOffsetX = 14;
                ResscalarOffsetY = 14;
                AplicarCapa.sprite = CapasPNG[11];
                ConCapa = true;
                break;
            case 20:
                txtMascara.text = NombreMascara + "Anubis [PS3]";
                ReescalarTamaño = 486;
                ResscalarOffsetX = -10;
                ResscalarOffsetY = 5;
                AplicarCapa.sprite = CapasPNG[12];
                ConCapa = true;
                break;
            case 21:
                txtMascara.text = NombreMascara + "Anubis [Blue Shadow]";
                ReescalarTamaño = 362;
                ResscalarOffsetX = 75;
                ResscalarOffsetY = 67;
                AplicarCapa.sprite = CapasPNG[13];
                ConCapa = true;
                break;
            case 22:
                txtMascara.text = NombreMascara + "Anubis [Magic]";
                ReescalarTamaño = 406;
                ResscalarOffsetX = 57;
                ResscalarOffsetY = 58;
                AplicarCapa.sprite = CapasPNG[14];
                ConCapa = true;
                break;
            case 23:
                txtMascara.text = NombreMascara + "Anubis [Shield]";
                ReescalarTamaño = 468;
                ResscalarOffsetX = 23;
                ResscalarOffsetY = 23;
                AplicarCapa.sprite = CapasPNG[15];
                ConCapa = true;
                break;
            case 24:
                txtMascara.text = NombreMascara + "Anubis [Heart]";
                ReescalarTamaño = 468;
                ResscalarOffsetX = 21;
                ResscalarOffsetY = 21;
                AplicarCapa.sprite = CapasPNG[16];
                ConCapa = true;
                break;
            case 25:
                txtMascara.text = NombreMascara + "Anubis [PS Fantasy]";
                ReescalarTamaño = 478;
                ResscalarOffsetX = 18;
                ResscalarOffsetY = 16;
                AplicarCapa.sprite = CapasPNG[17];
                ConCapa = true;
                break;
            case 26:
                txtMascara.text = NombreMascara + "Anubis [Ring]";
                ReescalarTamaño = 421;
                ResscalarOffsetX = 45;
                ResscalarOffsetY = 18;
                AplicarCapa.sprite = CapasPNG[18];
                ConCapa = true;
                break;
            case 27:
                txtMascara.text = NombreMascara + "Anubis [Star]";
                ReescalarTamaño = 460;
                ResscalarOffsetX = 26;
                ResscalarOffsetY = 26;
                AplicarCapa.sprite = CapasPNG[19];
                ConCapa = true;
                break;
            case 28:
                txtMascara.text = NombreMascara + "Anubis [Unicorn]";
                ReescalarTamaño = 332;
                ResscalarOffsetX = 90;
                ResscalarOffsetY = 156;
                AplicarCapa.sprite = CapasPNG[20];
                ConCapa = true;
                break;
            case 29:
                txtMascara.text = NombreMascara + "PS4 [Default]";
                MascaraOn = false;
                break;
        }

        AplicarMascara.gameObject.SetActive(MascaraOn);
        if (MascaraOn)
        {
            AplicarMascara.sprite = MascaraPNG[IdMask];
        }

        ActualizarIconos();
    }

    private void ActualizarIconos()
    {
        if (MascaraOn)
        {
            Iconos[0].sprite = Resources.Load("Demo/APP_M" + IdMask.ToString(), typeof(Sprite)) as Sprite;

            if (chkAplicarSistema.isOn)
                Iconos[1].sprite = Resources.Load("Demo/BRO_M" + IdMask.ToString(), typeof(Sprite)) as Sprite;
            else
                Iconos[1].sprite = Resources.Load("NPXS20102", typeof(Sprite)) as Sprite;

            if (chkCarpetaTransparente.isOn)
                Iconos[2].sprite = Resources.Load("NPXS20133_TP", typeof(Sprite)) as Sprite;
            else
            {
                if (chkAplicarSistema.isOn)
                    Iconos[2].sprite = Resources.Load("Demo/FOL_M" + IdMask.ToString(), typeof(Sprite)) as Sprite;
                else
                    Iconos[2].sprite = Resources.Load("NPXS20133", typeof(Sprite)) as Sprite;
            }
        }
        else
        {
            Iconos[0].sprite = Resources.Load("Demo/APP", typeof(Sprite)) as Sprite;
            Iconos[1].sprite = Resources.Load("NPXS20102", typeof(Sprite)) as Sprite;
            if (chkCarpetaTransparente.isOn)
                Iconos[2].sprite = Resources.Load("NPXS20133_TP", typeof(Sprite)) as Sprite;
            else
                Iconos[2].sprite = Resources.Load("NPXS20133", typeof(Sprite)) as Sprite;
        }
    }

    public static Texture2D Reescalar(Texture2D fuente, int NuevoTamaño)
    {
        fuente.filterMode = FilterMode.Trilinear;
        RenderTexture rt = RenderTexture.GetTemporary(NuevoTamaño, NuevoTamaño);
        rt.filterMode = FilterMode.Trilinear;
        RenderTexture.active = rt;
        Graphics.Blit(fuente, rt);
        Texture2D nuevaTex = new Texture2D(NuevoTamaño, NuevoTamaño);
        nuevaTex.ReadPixels(new Rect(0, 0, NuevoTamaño, NuevoTamaño), 0, 0);
        nuevaTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nuevaTex;
    }

    IEnumerator AplicandoPasoPaso(bool ConMascara, string juego)
    {
        string Errores = "";
        Color pixelBlanco = new Color(1f, 1f, 1f);

        if (juego.IndexOf("/system_ex") == 0)
        {
            // crear salida
            Sprite recurso = Resources.Load(juego.Substring(juego.Length - 9, 9), typeof(Sprite)) as Sprite;
            if (recurso != null)
            {
                if (ReescalarTamaño > 0)
                {
                    AplicarJuego.sprite = Sprite.Create(Reescalar(recurso.texture, ReescalarTamaño), new Rect(0, 0, ReescalarTamaño, ReescalarTamaño), new Vector2(0.0f, 0.0f), 1.0f);
                }
                else
                {
                    AplicarJuego.sprite = recurso;
                }

                Texture2D output = new Texture2D(512, 512);

                Color pMascara;
                Color pIcono;
                Color pCapa;
                Color pFinal;

                for (int i = 0; i < 512; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        if (ConMascara)
                        {
                            pMascara = AplicarMascara.sprite.texture.GetPixel(i, j);
                            pIcono = AplicarJuego.sprite.texture.GetPixel(i - ResscalarOffsetX, j - ResscalarOffsetY);
                            if (ConCapa)
                            {
                                pCapa = AplicarCapa.sprite.texture.GetPixel(i, j);

                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original + capa
                                        pFinal = Color.Lerp(pIcono, pCapa, pCapa.a / 1f);
                                        pFinal.a = Mathf.Min(pIcono.a + pCapa.a, 1f);
                                        output.SetPixel(i, j, pFinal);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara + capa
                                        pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                        output.SetPixel(i, j, pFinal);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara + capa
                                    pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                    output.SetPixel(i, j, pFinal);
                                }
                            }
                            else
                            {
                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original
                                        output.SetPixel(i, j, pIcono);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara
                                        output.SetPixel(i, j, pMascara);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara
                                    output.SetPixel(i, j, pMascara);
                                }
                            }
                        }
                        else
                        {
                            output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                        }
                    }
                }

                output.Apply();
                FlipTextureVertically(output);

                try
                {
                    File.Delete(juego + "/sce_sys/icon0.png");
                    File.WriteAllBytes(juego + "/sce_sys/icon0.png", output.EncodeToPNG());

                    if (Espanol)
                    {
                        txtDebug.text = juego.Substring(juego.Length - 9, 9) + " Listo";
                    }
                    else
                    {
                        txtDebug.text = juego.Substring(juego.Length - 9, 9) + " Done";
                    }

                    HuboCambios = true;
                }
                catch (System.Exception ex)
                {
                    txtDebug.text = ex.Message;
                    Errores += juego.Substring(juego.Length - 9, 9) + "\n";
                }
            }
        }
        else
        {
            if (File.Exists(juego + "/icon0.dds") || File.Exists(juego + "/icon0.png.bak"))
            {
                // crear salida
                if (File.Exists(juego + "/icon0.dds"))
                {
                    AplicarJuego.sprite = LeerIconoDDS(juego + "/icon0.dds", ReescalarTamaño);
                }
                else
                {
                    AplicarJuego.sprite = LeerImagenPNG(juego + "/icon0.png.bak", ReescalarTamaño);
                }

                Texture2D output = new Texture2D(512, 512);

                Color pMascara;
                Color pIcono;
                Color pCapa;
                Color pFinal;

                for (int i = 0; i < 512; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        if (ConMascara)
                        {
                            pMascara = AplicarMascara.sprite.texture.GetPixel(i, j);
                            pIcono = AplicarJuego.sprite.texture.GetPixel(i - ResscalarOffsetX, j - ResscalarOffsetY);
                            if (ConCapa)
                            {
                                pCapa = AplicarCapa.sprite.texture.GetPixel(i, j);

                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original + capa
                                        pFinal = Color.Lerp(pIcono, pCapa, pCapa.a / 1f);
                                        pFinal.a = 1f;
                                        output.SetPixel(i, j, pFinal);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara + capa
                                        pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                        output.SetPixel(i, j, pFinal);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara + capa
                                    pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                    output.SetPixel(i, j, pFinal);
                                }
                            }
                            else
                            {
                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original
                                        output.SetPixel(i, j, pIcono);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara
                                        output.SetPixel(i, j, pMascara);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara
                                    output.SetPixel(i, j, pMascara);
                                }
                            }
                        }
                        else
                        {
                            output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                        }
                    }
                }

                output.Apply();
                FlipTextureVertically(output);

                try
                {
                    File.Delete(juego + "/icon0.png");
                    File.WriteAllBytes(juego + "/icon0.png", output.EncodeToPNG());

                    CheckMultiplesIconos(juego);

                    if (Espanol)
                    {
                        txtDebug.text = juego.Substring(juego.Length - 9, 9) + " Listo";
                    }
                    else
                    {
                        txtDebug.text = juego.Substring(juego.Length - 9, 9) + " Done";
                    }

                    HuboCambios = true;
                }
                catch (System.Exception ex)
                {
                    txtDebug.text = ex.Message;
                    Errores += juego.Substring(juego.Length - 9, 9) + "\n";
                }
            }
        }

        yield return null;
        if (juego.IndexOf("/system_ex") == 0)
        {
            IconoPreview.sprite = LeerImagenPNG(juego + "/sce_sys/icon0.png", 512);
        }
        else
        {
            IconoPreview.sprite = LeerImagenPNG(juego + "/icon0.png", 512);
        }
    }

    IEnumerator Aplicando(bool ConMascara)
    {
        string Errores = "";
        Color pixelBlanco = new Color(1f, 1f, 1f);

        string AplicandoMascara = "Applying mask to: ";
        if (Espanol)
        {
            AplicandoMascara = "Aplicando máscara a: ";
        }
                
        // juegos/apps del usuario
        foreach (string juego in scaneo)
        {
            if (File.Exists(juego + "/icon0.dds") || File.Exists(juego + "/icon0.png.bak"))
            {
                // crear salida
                if (File.Exists(juego + "/icon0.dds"))
                {
                    AplicarJuego.sprite = LeerIconoDDS(juego + "/icon0.dds", ReescalarTamaño);
                }
                else
                {
                    AplicarJuego.sprite = LeerImagenPNG(juego + "/icon0.png.bak", ReescalarTamaño);
                }

                txtDebug.text = AplicandoMascara + juego.Substring(juego.Length - 9, 9);
                yield return null;

                Texture2D output = new Texture2D(512, 512);

                Color pMascara;
                Color pIcono;
                Color pCapa;
                Color pFinal;

                for (int i = 0; i < 512; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        if (ConMascara)
                        {
                            pMascara = AplicarMascara.sprite.texture.GetPixel(i, j);
                            pIcono = AplicarJuego.sprite.texture.GetPixel(i - ResscalarOffsetX, j - ResscalarOffsetY);
                            if (ConCapa)
                            {
                                pCapa = AplicarCapa.sprite.texture.GetPixel(i, j);

                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original + capa
                                        pFinal = Color.Lerp(pIcono, pCapa, pCapa.a / 1f);
                                        pFinal.a = 1f;
                                        output.SetPixel(i, j, pFinal);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara + capa
                                        pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                        output.SetPixel(i, j, pFinal);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara + capa
                                    pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                    output.SetPixel(i, j, pFinal);
                                }
                            }
                            else
                            {
                                if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                {
                                    if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                    {
                                        // poner pixel del icono original
                                        output.SetPixel(i, j, pIcono);
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara
                                        output.SetPixel(i, j, pMascara);
                                    }
                                }
                                else
                                {
                                    // poner pixel de la mascara
                                    output.SetPixel(i, j, pMascara);
                                }
                            }
                        }
                        else
                        {
                            output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                        }
                    }
                }

                output.Apply();
                FlipTextureVertically(output);

                try
                {
                    File.Delete(juego + "/icon0.png");
                    File.WriteAllBytes(juego + "/icon0.png", output.EncodeToPNG());

                    CheckMultiplesIconos(juego);
                }
                catch (System.Exception ex)
                {
                    txtDebug.text = ex.Message;
                    Errores += juego.Substring(juego.Length - 9, 9) + "\n";
                }
            }
        }

        // juegos/apps en disco externo
        if (scaneoExt != null)
        {
            foreach (string juegoExt in scaneoExt)
            {
                if (File.Exists(juegoExt + "/icon0.dds") || File.Exists(juegoExt + "/icon0.png.bak"))
                {
                    // crear salida
                    if (File.Exists(juegoExt + "/icon0.dds"))
                    {
                        AplicarJuego.sprite = LeerIconoDDS(juegoExt + "/icon0.dds", ReescalarTamaño);
                    }
                    else
                    {
                        AplicarJuego.sprite = LeerImagenPNG(juegoExt + "/icon0.png.bak", ReescalarTamaño);
                    }

                    txtDebug.text = AplicandoMascara + juegoExt.Substring(juegoExt.Length - 9, 9);
                    yield return null;

                    Texture2D output = new Texture2D(512, 512);

                    Color pMascara;
                    Color pIcono;
                    Color pCapa;
                    Color pFinal;

                    for (int i = 0; i < 512; i++)
                    {
                        for (int j = 0; j < 512; j++)
                        {
                            if (ConMascara)
                            {
                                pMascara = AplicarMascara.sprite.texture.GetPixel(i, j);
                                pIcono = AplicarJuego.sprite.texture.GetPixel(i - ResscalarOffsetX, j - ResscalarOffsetY);
                                if (ConCapa)
                                {
                                    pCapa = AplicarCapa.sprite.texture.GetPixel(i, j);

                                    if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                    {
                                        if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                        {
                                            // poner pixel del icono original + capa
                                            pFinal = Color.Lerp(pIcono, pCapa, pCapa.a / 1f);
                                            pFinal.a = 1f;
                                            output.SetPixel(i, j, pFinal);
                                        }
                                        else
                                        {
                                            // poner pixel de la mascara + capa
                                            pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                            output.SetPixel(i, j, pFinal);
                                        }
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara + capa
                                        pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                        output.SetPixel(i, j, pFinal);
                                    }
                                }
                                else
                                {
                                    if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                    {
                                        if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                        {
                                            // poner pixel del icono original
                                            output.SetPixel(i, j, pIcono);
                                        }
                                        else
                                        {
                                            // poner pixel de la mascara
                                            output.SetPixel(i, j, pMascara);
                                        }
                                    }
                                    else
                                    {
                                        // poner pixel de la mascara
                                        output.SetPixel(i, j, pMascara);
                                    }
                                }
                            }
                            else
                            {
                                output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                            }
                        }
                    }

                    output.Apply();
                    FlipTextureVertically(output);

                    try
                    {
                        File.Delete(juegoExt + "/icon0.png");
                        File.WriteAllBytes(juegoExt + "/icon0.png", output.EncodeToPNG());

                        CheckMultiplesIconos(juegoExt);
                    }
                    catch (System.Exception ex)
                    {
                        txtDebug.text = ex.Message;
                        Errores += juegoExt.Substring(juegoExt.Length - 9, 9) + "\n";
                    }
                }
            }
        }

        // apps del sistema
        if (!FalloRW)
        {
            if (chkAplicarSistema.isOn)
            {
                foreach (string appSis in scaneoSis)
                {
                    if (chkCarpetaTransparente.isOn)
                    {
                        if (appSis.Substring(appSis.Length - 9, 9) == "NPXS20133")
                        {
                            continue;
                        }
                    }

                    // crear salida
                    Sprite recurso = Resources.Load(appSis.Substring(appSis.Length - 9, 9), typeof(Sprite)) as Sprite;
                    if (recurso != null)
                    {
                        if (ReescalarTamaño > 0)
                        {
                            AplicarJuego.sprite = Sprite.Create(Reescalar(recurso.texture, ReescalarTamaño), new Rect(0, 0, ReescalarTamaño, ReescalarTamaño), new Vector2(0.0f, 0.0f), 1.0f);
                        }
                        else
                        {
                            AplicarJuego.sprite = recurso;
                        }
                        
                        txtDebug.text = AplicandoMascara + appSis.Substring(appSis.Length - 9, 9);
                        yield return null;

                        Texture2D output = new Texture2D(512, 512);

                        Color pMascara;
                        Color pIcono;
                        Color pCapa;
                        Color pFinal;

                        for (int i = 0; i < 512; i++)
                        {
                            for (int j = 0; j < 512; j++)
                            {
                                if (ConMascara)
                                {
                                    pMascara = AplicarMascara.sprite.texture.GetPixel(i, j);
                                    pIcono = AplicarJuego.sprite.texture.GetPixel(i - ResscalarOffsetX, j - ResscalarOffsetY);
                                    if (ConCapa)
                                    {
                                        pCapa = AplicarCapa.sprite.texture.GetPixel(i, j);

                                        if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                        {
                                            if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                            {
                                                // poner pixel del icono original + capa
                                                pFinal = Color.Lerp(pIcono, pCapa, pCapa.a / 1f);
                                                pFinal.a = Mathf.Min(pIcono.a + pCapa.a, 1f);
                                                output.SetPixel(i, j, pFinal);
                                            }
                                            else
                                            {
                                                // poner pixel de la mascara + capa
                                                pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                                output.SetPixel(i, j, pFinal);
                                            }
                                        }
                                        else
                                        {
                                            // poner pixel de la mascara + capa
                                            pFinal = Color.Lerp(pMascara, pCapa, pCapa.a / 1f);
                                            output.SetPixel(i, j, pFinal);
                                        }
                                    }
                                    else
                                    {
                                        if (pixelBlanco == new Color(pMascara.r, pMascara.g, pMascara.b))
                                        {
                                            if (pMascara.a > 0.50f && pMascara.a < 0.51f)
                                            {
                                                // poner pixel del icono original
                                                output.SetPixel(i, j, pIcono);
                                            }
                                            else
                                            {
                                                // poner pixel de la mascara
                                                output.SetPixel(i, j, pMascara);
                                            }
                                        }
                                        else
                                        {
                                            // poner pixel de la mascara
                                            output.SetPixel(i, j, pMascara);
                                        }
                                    }
                                }
                                else
                                {
                                    output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                                }
                            }
                        }

                        output.Apply();
                        FlipTextureVertically(output);

                        try
                        {
                            File.Delete(appSis + "/sce_sys/icon0.png");
                            File.WriteAllBytes(appSis + "/sce_sys/icon0.png", output.EncodeToPNG());
                        }
                        catch (System.Exception ex)
                        {
                            txtDebug.text = ex.Message;
                            Errores += appSis.Substring(appSis.Length - 9, 9) + "\n";
                        }
                    }
                }
            }

            // icono de carpeta transparente
            if (chkCarpetaTransparente.isOn)
            {
                AplicarJuego.sprite = Resources.Load("NPXS20133_TP", typeof(Sprite)) as Sprite;
                if (AplicarJuego.sprite != null)
                {
                    string appSisFolder = "/system_ex/app/NPXS20133";

                    // crear salida
                    txtDebug.text = AplicandoMascara + "NPXS20133";
                    yield return null;

                    Texture2D output = new Texture2D(512, 512);

                    for (int i = 0; i < 512; i++)
                    {
                        for (int j = 0; j < 512; j++)
                        {
                            output.SetPixel(i, j, AplicarJuego.sprite.texture.GetPixel(i, j));
                        }
                    }

                    output.Apply();
                    FlipTextureVertically(output);

                    try
                    {
                        File.Delete(appSisFolder + "/sce_sys/icon0.png");
                        File.WriteAllBytes(appSisFolder + "/sce_sys/icon0.png", output.EncodeToPNG());
                    }
                    catch (System.Exception ex)
                    {
                        txtDebug.text = ex.Message;
                        Errores += "NPXS20133\n";
                    }
                }
            }
        }

        PanelIconos.SetActive(true);
        
        if (Errores != "")
        {
            if (Espanol)
            {
                txtDebug.text = "Algunos íconos fallaron (inténtalo de nuevo después de reiniciar)";
            }
            else
            {
                txtDebug.text = "Some icons failed (try again after rebooting)";
            }
        }
        else
        {
            if (Espanol)
            {
                txtDebug.text = "La máscara fue aplicada, reinicia tu PS4 para que veas los cambios\no cambia de máscara (L1 o R1) y aplica de nuevo";
            }
            else
            {
                txtDebug.text = "The mask was applied, reboot the PS4 to see the changes\nor change the mask (L1 or R1) and apply again";
            }
        }

        yield return new WaitForSeconds(3f);
        EstoyAplicando = false;
    }

    private void CheckMultiplesIconos(string juego)
    {
        string[] scanIcons = Directory.GetFileSystemEntries(juego + "/", "icon0_*.png");
        if (scanIcons.Length > 0)
        {
            foreach (string iconoMulti in scanIcons)
            {
                File.Copy(juego + "/icon0.png", iconoMulti, true);
            }
        }
    }

    Sprite LeerIconoDDS(string caminoFoto, int NuevoTamaño)
    {
        byte[] bytes = File.ReadAllBytes(caminoFoto);
        Texture2D texture = LoadTextureDXT(bytes, TextureFormat.DXT1);

        if (NuevoTamaño > 0)
        {
            return Sprite.Create(Reescalar(texture, NuevoTamaño), new Rect(0, 0, NuevoTamaño, NuevoTamaño), new Vector2(0.0f, 0.0f), 1.0f);
        }
        else
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);
        }
    }

    Sprite LeerImagenPNG(string caminoFoto, int NuevoTamaño) // y voltear
    {
        byte[] bytes = File.ReadAllBytes(caminoFoto);
        Texture2D texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);

        FlipTextureVertically(texture);

        if (NuevoTamaño > 0)
        {
            return Sprite.Create(Reescalar(texture, NuevoTamaño), new Rect(0, 0, NuevoTamaño, NuevoTamaño), new Vector2(0.0f, 0.0f), 1.0f);
        }
        else
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);
        }
    }

    public static Texture2D LoadTextureDXT(byte[] ddsBytes, TextureFormat textureFormat)
    {
        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
            return null;

        int height = ddsBytes[13] * 256 + ddsBytes[12];
        int width = ddsBytes[17] * 256 + ddsBytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
        System.Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

        Texture2D texture = new Texture2D(width, height, textureFormat, false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply(false);

        return (texture);
    }

    public static void FlipTextureVertically(Texture2D original)
    {
        var originalPixels = original.GetPixels();
        var newPixels = new Color[originalPixels.Length];
        var width = original.width;
        var rows = original.height;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
            }
        }

        original.SetPixels(newPixels);
        original.Apply();
    }
}
