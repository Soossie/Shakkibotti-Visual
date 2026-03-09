using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public class ShakkiLogiikka : MonoBehaviour
{
    
    [DllImport("libShakkibotti")]
    private static extern void Aloitus();

    [DllImport("libShakkibotti")]
    private static extern IntPtr Loop(int koneenVari, string syotto);

    [DllImport("libShakkibotti")]
    private static extern IntPtr PelaajanSiirrot();
    
    private bool _lopetus = false;
    public bool _onLaillinen;

    private int _pelaajanVari = -1;
    private int _koneenVari;
    private int _vuoro = 0; // 0 = valkoinen, 1 = musta

    private string _siirtolistaStr;
    private string[] _siirtolista;
    private string uusiSiirto;
    private GameObject _siirrettavaNappula;
    private GameObject _vihollisenSiirrettavaNappula;
    private GameObject ColorPickerCanvas;
    private GameObject syotavaNappula;
    
    private RaycastHit hit;

    private Ray playerRay;
    private Ray enemyRay;
    
    private Vector2 mousePosition;
    private GameObject aloitusRuutu;
    private GameObject lopetusRuutu;
    
    private string _siirto;
    
    void Start()
    {
        ColorPickerCanvas = GameObject.Find("ColorPickerCanvas");
        ColorPickerCanvas.GetComponent<Canvas>().enabled = true;
        GenerateGrid();
        GeneratePieces();
        Aloitus();
    }

    private void Update()
    {
        if (_pelaajanVari == -1)
            return;
        mousePosition = Mouse.current.position.ReadValue();
        playerRay = Camera.main.ScreenPointToRay(mousePosition);
        if (_siirrettavaNappula)
            _siirrettavaNappula.transform.position = new Vector3(playerRay.direction.x, 0.4f, playerRay.direction.z);
        
        Debug.DrawRay(playerRay.origin, playerRay.direction * 100, Color.red);
    }

    private IEnumerator SiirtoLogiikka()
    {
        _koneenVari = 1 - _pelaajanVari; // kone saa vastakkaisen värin
        if (!_lopetus)
        {
            Debug.Log("Vuoro: " + (_vuoro == 0 ? "Valkoinen" : "Musta"));
            if (_vuoro == _pelaajanVari)
            {
                yield return StartCoroutine(PelaajanSiirto());
                _siirto = KieliMuunnos(_siirto);
                Debug.Log("Siirto oikeassa muodossa: " + _siirto);
                IntPtrToString(Loop(_koneenVari, _siirto)); //Suorittaa pelaajan siirron (sama kuin _siirto)
            }
            else
            {
                //Koneen vuoro, syötetään tyhjä string, koska koneen siirto lasketaan Loop-funktiossa
                _siirto = IntPtrToString(Loop(_koneenVari, "")); 
                Debug.Log("Koneen siirto: " + _siirto);
                VastustajanSiirto();
            }
            
            _vuoro = 1 - _vuoro; //vaihda vuoroa
            StartCoroutine(SiirtoLogiikka()); //Rekursioaskel
        }

        else //Kantatapaus
        {
            Debug.Log("Peli loppui!");
        }

        yield return null;
    }

    private IEnumerator PelaajanSiirto()
    {
        _siirtolistaStr = IntPtrToString(PelaajanSiirrot());
        _siirtolista = _siirtolistaStr.Split(",");
        Debug.Log("Pelaajan siirrot:");
        foreach (var siirto in _siirtolista)
        {
            Debug.Log(siirto);
        }
        
        if (_siirtolista.Length == 0)
        {
            Debug.Log("Pelaajalla ei laillisia siirtoja, peli loppuu!");
            _lopetus = true;
            yield break;
        }
        
        Debug.Log("Odotetaan siirtoa...");
        yield return new WaitUntil(() => _onLaillinen); //Tässä vaiheessa pelaaja siirtelee
        Debug.Log("Siirto laillinen: " + _siirto);
        if (_siirrettavaNappula.name.Substring(1, 1) != "S")
            _siirto = _siirrettavaNappula.name.Substring(1, 1) + _siirto;
        _siirrettavaNappula.transform.position = lopetusRuutu.transform.position;
        if (syotavaNappula)
        {
            Destroy(syotavaNappula);
            syotavaNappula = null;
        }
        _siirrettavaNappula.GetComponent<BoxCollider>().enabled = true;
        _siirrettavaNappula = null;
        _onLaillinen = false;
    }

    private void VastustajanSiirto()
    {
        lopetusRuutu = GameObject.Find(_siirto.Substring(4, 2));
        aloitusRuutu = GameObject.Find(_siirto.Substring(0, 2));
        Debug.Log("Aloitusruutu: " + aloitusRuutu.name);
        Debug.Log("Lopetusruutu: " + lopetusRuutu.name);
        enemyRay = new Ray(aloitusRuutu.transform.position + new Vector3(0, 0.5f, 0), Vector3.down);
        if (Physics.Raycast(enemyRay, out hit))
        {
            Debug.DrawRay(enemyRay.origin, enemyRay.direction * 100, Color.blue, 2f);
            if (hit.collider.gameObject.CompareTag("MustaNappula"))
            {
                _vihollisenSiirrettavaNappula = hit.collider.gameObject;
                Debug.Log("Siirretään koneen nappulaa: " + hit.collider.gameObject.name);
                enemyRay = new Ray(lopetusRuutu.transform.position + new Vector3(0, 0.5f, 0), Vector3.down);
                if (Physics.Raycast(enemyRay, out hit))
                {
                    if (hit.collider.gameObject.CompareTag("ValkoinenNappula"))
                    {
                        Debug.Log("Kone syö pelaajan nappulan: " + hit.collider.gameObject.name);
                        Destroy(hit.collider.gameObject);
                    }
                }
                _vihollisenSiirrettavaNappula.transform.position = lopetusRuutu.transform.position + new Vector3(0, 0.1f, 0);
                _vihollisenSiirrettavaNappula = null;
                lopetusRuutu = null;
                aloitusRuutu = null;
            }
        }
    }

    public void KysyPelaajanVari(int vari)
    {
        _pelaajanVari = vari;
        ColorPickerCanvas.GetComponent<Canvas>().enabled = false;
        StartCoroutine(SiirtoLogiikka());
    }

    public void OnkoLaillinenSiirto(InputAction.CallbackContext context)
    {
        if (_vuoro != _pelaajanVari)
            return;
        if (context.started)
        {
            if (Physics.Raycast(playerRay, out hit))
            {
                if (hit.collider.gameObject.CompareTag("ValkoinenNappula"))
                {
                    _siirrettavaNappula = hit.collider.gameObject;
                    _siirrettavaNappula.GetComponent<BoxCollider>().enabled = false;
                    
                    Physics.Raycast(playerRay, out hit);
                    aloitusRuutu = hit.collider.gameObject;
                    aloitusRuutu.transform.position = hit.collider.gameObject.transform.position + new Vector3(0, 0.1f, 0);
                    Debug.Log("Original position: " + aloitusRuutu.name);
                    Debug.Log("Hit something: " + _siirrettavaNappula.name);
                }
            }
        }
        
        else if (context.canceled)
        {
            if (_siirrettavaNappula)
            {
                if (Physics.Raycast(playerRay, out hit))
                {
                    if (hit.collider.gameObject.CompareTag("MustaNappula"))
                    {
                        syotavaNappula = hit.collider.gameObject;
                        hit.collider.gameObject.GetComponent<BoxCollider>().enabled = false;
                        Physics.Raycast(playerRay, out hit);
                        syotavaNappula.GetComponent<BoxCollider>().enabled = true;
                    }
                    if (hit.collider.gameObject.CompareTag("Ruutu"))
                    {
                        Debug.Log("Osui ruutuun: " + hit.collider.gameObject.name);
                        lopetusRuutu = hit.collider.gameObject;
                        lopetusRuutu.transform.position = hit.collider.gameObject.transform.position + new Vector3(0, 0.1f, 0);
                        uusiSiirto = aloitusRuutu.name + "->" + lopetusRuutu.name;
                        
                        
                        Debug.Log("Uusi siirto: " + uusiSiirto);

                        foreach (var siirto in _siirtolista)
                        {
                            //Debug.Log("Tarkasteltu siirto: " + siirto);
                            if (uusiSiirto == siirto)
                            {
                                _siirto = siirto;
                                _onLaillinen = true;
                                return;
                            }
                        }

                        //Jos ei ole laillinen siirto, palautetaan nappula alkuperäiseen paikkaan
                        _siirrettavaNappula.transform.position = aloitusRuutu.transform.position;
                        _siirrettavaNappula.GetComponent<BoxCollider>().enabled = true;
                        _siirrettavaNappula = null;
                        aloitusRuutu = null;
                        lopetusRuutu = null;
                        Debug.Log("Laiton siirto " + uusiSiirto);
                    }
                }
                
                //Jos ei osunut ruutuun, palautetaan nappula alkuperäiseen paikkaan
                else
                {
                    _siirrettavaNappula.transform.position = aloitusRuutu.transform.position;
                    _siirrettavaNappula.GetComponent<BoxCollider>().enabled = true;
                    _siirrettavaNappula = null;
                    aloitusRuutu = null;
                    lopetusRuutu = null;
                    Debug.Log("Ei osunut ruutuun");
                }
            }
        }
    }
    
    //Muuttaa IntPtrin stringiksi, jos IntPtr on nolla, palauttaa tyhjän stringin (C++ -> C# -muunnos)
    private static string IntPtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        return Marshal.PtrToStringUTF8(ptr);
    }

    //Muuttaa siirron oikeaan formaattiin shakkibotille
    private string KieliMuunnos(string siirto)
    {
        if (siirto.Length == 6)
        {
            string alkusarake = ((char)('a' + int.Parse(siirto[0].ToString()))).ToString();
            string alkurivi = (8 - int.Parse(siirto[1].ToString())).ToString();
            string loppusarake = ((char)('a' + int.Parse(siirto[4].ToString()))).ToString();
            string loppurivi = (8 - int.Parse(siirto[5].ToString())).ToString();
            return alkusarake + alkurivi + "-" + loppusarake + loppurivi;

        }
        else
        {
            string alkusarake = ((char)('a' + int.Parse(siirto[1].ToString()))).ToString();
            string alkurivi = (8 - int.Parse(siirto[2].ToString())).ToString();
            string loppusarake = ((char)('a' + int.Parse(siirto[5].ToString()))).ToString();
            string loppurivi = (8 - int.Parse(siirto[6].ToString())).ToString();
            return siirto[0] + alkusarake + alkurivi + "-" + loppusarake + loppurivi;

        }
    }
    
// a0 = -0.7595f, 0.21f (ei tarkka), -0.7595f
    void GenerateGrid()
    {
        for (int z = 0; z < 8; z++)
        {
            for (int x = 0; x < 8; x++)
            {
                var spawnedTile = Instantiate(Resources.Load("Prefabs/Ruutu"), new Vector3(-0.7595f + 0.6f + 0.217f * x, 0.2f, -0.7595f + 0.217f * z), Quaternion.identity);
                spawnedTile.name = $"{x}{z}";
            }
        }
    }

    void GeneratePieces()
    {
        Quaternion blackrotation = Quaternion.Euler(-90, 90, 0);
        Quaternion whiterotation = Quaternion.Euler(-90, -90, 0);
        
        
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/VS"), 
                GameObject.Find(x + "1").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VT"), GameObject.Find("00").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VR"), GameObject.Find("10").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VL"), GameObject.Find("20").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VD"), GameObject.Find("30").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VK"), GameObject.Find("40").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VL"), GameObject.Find("50").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VR"), GameObject.Find("60").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        Instantiate(Resources.Load("Prefabs/VT"), GameObject.Find("70").transform.position + new Vector3(0, 0.1f, 0), whiterotation);
        
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/MS"), 
                GameObject.Find(x + "6").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MT"), GameObject.Find("07").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MR"), GameObject.Find("17").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/ML"), GameObject.Find("27").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MD"), GameObject.Find("37").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MK"), GameObject.Find("47").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/ML"), GameObject.Find("57").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MR"), GameObject.Find("67").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
        Instantiate(Resources.Load("Prefabs/MT"), GameObject.Find("77").transform.position + new Vector3(0, 0.1f, 0), blackrotation);
    }
    
    //funktio siirron suorittamiselle (onko teleport siirto vai sulava siirto)
}
