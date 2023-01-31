using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PS4Manual : MonoBehaviour {

    public Image IconoPS4;
    public Image IconoUSB;
    public Text txtMensaje;

    private string[] scaneo;
    private List<string> todosIconosPs4 = new List<string>();
    private int PosPs4 = 0;
    private int PosUsb = 0;

    private bool Acelerar = false;
    private bool Paso = true;

    string Destino = "";
    string Origen = "";

	void Start ()
    {
        scaneo = Directory.GetFileSystemEntries("/user/appmeta/");
        foreach (string juego in scaneo)
        {
            if (File.Exists(juego + "/icon0.png"))
            {
                todosIconosPs4.Add(juego);
            }
        }

        if (Directory.Exists("/user/appmeta/external/"))
        {
            scaneo = Directory.GetFileSystemEntries("/user/appmeta/external/");
            foreach (string juego in scaneo)
            {
                if (File.Exists(juego + "/icon0.png"))
                {
                    todosIconosPs4.Add(juego);
                }
            }
        }

        if (!PS4Iconos.instancia.FalloRW)
        {
            scaneo = Directory.GetFileSystemEntries("/system_ex/app/");
            foreach (string juego in scaneo)
            {
                if (File.Exists(juego + "/sce_sys/icon0.png"))
                {
                    todosIconosPs4.Add(juego + "/sce_sys");
                }
            }
        }

        scaneo = null;

        if (PS4Iconos.instancia.Espanol)
        {
            transform.GetChild(1).GetComponent<Text>().text = "Seleccione el icono de su PS4 que desea reemplazar manualmente";
            transform.GetChild(3).GetComponent<Text>().text = "Reemplazar con:";
            transform.GetChild(5).GetComponentInChildren<Text>().text = "Aplicar";
            transform.GetChild(6).GetComponentInChildren<Text>().text = "Recargar USB";
            transform.GetChild(7).GetComponentInChildren<Text>().text = "Mantener para velocidad";
        }

        CargarIconoPS4();
        RecargarUSB();
	}

    public void OnEnable()
    {
        CargarIconoPS4();
    }

    private void RecargarUSB()
    {
        if (Directory.Exists("/mnt/usb0/ICONS/"))
        {
            scaneo = Directory.GetFileSystemEntries("/mnt/usb0/ICONS/", "*.png");
            PosUsb = 0;
            CargarIconosUSB();
        }
        else if (Directory.Exists("/mnt/usb1/ICONS/"))
        {
            scaneo = Directory.GetFileSystemEntries("/mnt/usb1/ICONS/", "*.png");
            PosUsb = 0;
            CargarIconosUSB();
        }
        else if (Directory.Exists("/data/ICONS/"))
        {
            scaneo = Directory.GetFileSystemEntries("/data/ICONS/", "*.png");
            PosUsb = 0;
            CargarIconosUSB();
        }
        else
        {
            if (PS4Iconos.instancia.Espanol)
            {
                txtMensaje.text = "Inserta una memoria USB con una carpeta llamada ICONS con tus iconos prefabricados y usa Recargar el USB";
            }
            else
            {
                txtMensaje.text = "Insert a USB stick with a folder named ICONS with your pre-made icons and use Reload USB";
            }
        }
    }
	
	void Update ()
    {
        // acelerar
        if (Application.platform == RuntimePlatform.PS4)
        {
            Acelerar = Input.GetAxis("joystick1_left_trigger") != 0;
        }
        else
        {
            Acelerar = Input.GetKey(KeyCode.LeftShift);
        }

        // mover iconos del PS4
        if ((Input.GetKey(KeyCode.Joystick1Button4) || Input.GetKey(KeyCode.Q)) && Paso)
        {
            if (PosPs4 > 0)
            {
                Paso = false;

                PosPs4--;
                CargarIconoPS4();

                StartCoroutine(SeguirPasando());
            }
        }
        if ((Input.GetKey(KeyCode.Joystick1Button5) || Input.GetKey(KeyCode.E)) && Paso)
        {
            if (PosPs4 < todosIconosPs4.Count - 1)
            {
                Paso = false;

                PosPs4++;
                CargarIconoPS4();

                StartCoroutine(SeguirPasando());
            }
        }

        // mover iconos del USB
        if ((Input.GetAxis("dpad1_horizontal") < 0 || Input.GetKey(KeyCode.A)) && Paso)
        {
            if (scaneo != null && PosUsb > 0)
            {
                Paso = false;

                PosUsb--;
                CargarIconosUSB();

                StartCoroutine(SeguirPasando());
            }
        }
        if ((Input.GetAxis("dpad1_horizontal") > 0 || Input.GetKey(KeyCode.D)) && Paso)
        {
            if (scaneo != null && PosUsb < scaneo.Length - 1)
            {
                Paso = false;

                PosUsb++;
                CargarIconosUSB();

                StartCoroutine(SeguirPasando());
            }
        }

        // Aplicar
        if (Origen != "")
        {
            if (Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.X))
            {
                try
                {
                    File.Copy(Origen, Destino, true);
                    CargarIconoPS4();

                    CheckMultiplesIconos(todosIconosPs4[PosPs4]);

                    PS4Iconos.instancia.HuboCambios = true;
                }
                catch (System.Exception ex)
                {
                    txtMensaje.text = ex.Message;
                }
            }
        }

        // Recargar USB
        if (Input.GetKeyDown(KeyCode.Joystick1Button3) || Input.GetKeyDown(KeyCode.R))
        {
            RecargarUSB();
        }
	}

    private void CheckMultiplesIconos(string juego)
    {
        string[] scanIcons = Directory.GetFileSystemEntries(juego + "/", "icon0_*.png");
        if (scanIcons.Length > 0)
        {
            foreach (string iconoMulti in scanIcons)
            {
                File.Copy(Origen, iconoMulti, true);
            }
        }
    }

    IEnumerator SeguirPasando()
    {
        if (Acelerar)
        {
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        Paso = true;
    }

    void CargarIconoPS4()
    {
        if (todosIconosPs4.Count > 0)
        {
            if (scaneo != null)
            {
                txtMensaje.text = "";
            }
            
            IconoPS4.sprite = LeerImagenPNG(todosIconosPs4[PosPs4] + "/icon0.png");
            if (todosIconosPs4[PosPs4].IndexOf("sce_sys") > 0)
            {
                IconoPS4.GetComponentInChildren<Text>().text = todosIconosPs4[PosPs4].Substring(todosIconosPs4[PosPs4].Length - 17, 9);
            }
            else
            {
                IconoPS4.GetComponentInChildren<Text>().text = todosIconosPs4[PosPs4].Substring(todosIconosPs4[PosPs4].Length - 9, 9);
            }
            
            Destino = todosIconosPs4[PosPs4] + "/icon0.png";
        }
    }

    void CargarIconosUSB()
    {
        txtMensaje.text = "";
        Origen = "";

        try
        {
            IconoUSB.sprite = LeerImagenPNG(scaneo[PosUsb]);
            IconoUSB.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(scaneo[PosUsb]);

            Origen = scaneo[PosUsb];
        }
        catch (System.Exception ex)
        {
            txtMensaje.text = ex.Message;
            Origen = "";
        }      
    }

    Sprite LeerImagenPNG(string caminoFoto)
    {
        byte[] bytes = File.ReadAllBytes(caminoFoto);
        Texture2D texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);
    }
}
