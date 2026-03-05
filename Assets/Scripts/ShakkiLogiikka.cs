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
    public bool _onLaillinen = false;

    private int _pelaajanVari;
    private int _koneenVari;
    private int _vuoro = 0; // 0 = valkoinen, 1 = musta

    private string _siirtolistaStr;
    private string[] _siirtolista;
    private GameObject _siirrettavaNappula;
    
    
    private string _siirto;
    
    void Start()
    {
        GenerateGrid();
        GeneratePieces();
        Aloitus();
        //kysy pelaajan väri
        _koneenVari = 1 - _pelaajanVari; // kone saa vastakkaisen värin
        StartCoroutine(SiirtoLogiikka());
    }

    private void Update()
    {
        if (_siirrettavaNappula)
            _siirrettavaNappula.transform.position = Camera.main.ScreenToWorldPoint
                (Mouse.current.position.ReadValue());
    }

    private IEnumerator SiirtoLogiikka()
    {
        if (!_lopetus) 
        {
            Debug.Log("Pelaajan väri: " + (_pelaajanVari == 0 ? "Valkoinen" : "Musta"));
            Debug.Log("Vuoro: " + (_vuoro == 0 ? "Valkoinen" : "Musta"));
            if (_vuoro == _pelaajanVari)
            {
                yield return StartCoroutine(PelaajanSiirto());
                IntPtrToString(Loop(_koneenVari, _siirto)); //Suorittaa pelaajan siirron
            }
            else 
                Loop(_koneenVari, ""); //Koneen vuoro, syötetään tyhjä string, koska koneen siirto lasketaan Loop-funktiossa
            
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
        //tässä pitää katsoa onko lista tyhjä, jos niin häviö sekä häviökonteksti?
        Debug.Log("Odotetaan siirtoa...");
        yield return new WaitUntil(() => _onLaillinen); //Tässä vaiheessa pelaaja siirtelee
        //siirrä nappula 
        //_siirto = tulee pelistä
        _onLaillinen = false;
    }

    public void OnkoLaillinenSiirto(InputAction.CallbackContext context)
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Debug.Log(mousePosition); // ei vielä toimi
        if (context.started)
        {
            if (!_siirrettavaNappula)
            {
                foreach (GameObject nappula in GameObject.FindGameObjectsWithTag("Nappula"))
                {
                    if (math.abs(nappula.transform.position.x - mousePosition.x) < 0.217f
                        && math.abs(nappula.transform.position.z - mousePosition.y) < 0.217f)
                    {
                        Debug.Log("Siirretään");
                        _siirrettavaNappula = nappula;
                    }
                }
            }
        }
        
        else if (context.canceled)
        {
            foreach (GameObject ruutu in GameObject.FindGameObjectsWithTag("Ruutu"))
            {
                if (math.abs(ruutu.transform.position.x - mousePosition.x) < 0.217f
                    && math.abs(ruutu.transform.position.z - mousePosition.y) < 0.217f)
                    foreach (var siirto in _siirtolista)
                        if (_siirrettavaNappula.name == siirto.Substring(0, 2) 
                            && ruutu.name == siirto.Substring(2, 2))
                        {
                            _siirto = siirto;
                            _siirrettavaNappula = null;
                            _onLaillinen = true;
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
    
// a0 = -0.7595f, 0.21f (ei tarkka), -0.7595f
    void GenerateGrid()
    {
        for (int z = 0; z < 8; z++)
        {
            for (int x = 0; x < 8; x++)
            {
                var spawnedTile = Instantiate(Resources.Load("Prefabs/Ruutu"), new Vector3(-0.7595f + 0.6f + 0.217f * x, 0.3f, -0.7595f + 0.217f * z), Quaternion.identity);
                spawnedTile.name = $"{x}{z}";
            }
        }
    }

    void GeneratePieces()
    {
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/VS"), 
                GameObject.Find(x + "1").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VT"), GameObject.Find("00").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VR"), GameObject.Find("10").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VL"), GameObject.Find("20").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VD"), GameObject.Find("30").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VK"), GameObject.Find("40").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VL"), GameObject.Find("50").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VR"), GameObject.Find("60").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VT"), GameObject.Find("70").transform.position, Quaternion.identity);
        
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/MS"), 
                GameObject.Find(x + "6").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MT"), GameObject.Find("07").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MR"), GameObject.Find("17").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/ML"), GameObject.Find("27").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MD"), GameObject.Find("37").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MK"), GameObject.Find("47").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/ML"), GameObject.Find("57").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MR"), GameObject.Find("67").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MT"), GameObject.Find("77").transform.position, Quaternion.identity);
    }
    
    //funktio siirron suorittamiselle (onko teleport siirto vai sulava siirto)
}
