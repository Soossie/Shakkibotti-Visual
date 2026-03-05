using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

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

    private IEnumerator SiirtoLogiikka()
    {
        if (!_lopetus) 
        {
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
        yield return new WaitUntil(() => _onLaillinen); //Tässä vaiheessa pelaaja siirtelee
        //siirrä nappula 
        //_siirto = tulee pelistä
        _onLaillinen = false;
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
                var spawnedTile = Instantiate(Resources.Load("Prefabs/Ruutu"), new Vector3(-0.7595f + 0.217f * x, 0.3f, -0.7595f + 0.217f * z), Quaternion.identity);
                spawnedTile.name = $"Tile {x}, {z}";
            }
        }
    }

    void GeneratePieces()
    {
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/VS"), 
                new Vector3(-0.7595f + 0.217f * x, 0.3f, -0.7595f + 0.217f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VT"), GameObject.Find("Tile 0,0").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VR"), GameObject.Find("Tile 0,0").transform.position, Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VL"), new Vector3(-0.7595f + 0.217f * 2, 0.3f, -0.7595f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VD"), new Vector3(-0.7595f + 0.217f * 3, 0.3f, -0.7595f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VK"), new Vector3(-0.7595f + 0.217f * 4, 0.3f, -0.7595f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VL"), new Vector3(-0.7595f + 0.217f * 5, 0.3f, -0.7595f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VR"), new Vector3(-0.7595f + 0.217f * 6, 0.3f, -0.7595f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/VT"), new Vector3(-0.7595f + 0.217f * 7, 0.3f, -0.7595f), Quaternion.identity);
        
        for (int x = 0; x < 8; x++)
            Instantiate(Resources.Load("Prefabs/MS"), 
                new Vector3(-0.7595f + 0.217f * x, 0.3f, -0.7595f + 0.217f * 6), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MT"), new Vector3(-0.7595f, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MR"), new Vector3(-0.7595f + 0.217f, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/ML"), new Vector3(-0.7595f + 0.217f * 2, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MD"), new Vector3(-0.7595f + 0.217f * 3, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MK"), new Vector3(-0.7595f + 0.217f * 4, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/ML"), new Vector3(-0.7595f + 0.217f * 5, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MR"), new Vector3(-0.7595f + 0.217f * 6, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/MT"), new Vector3(-0.7595f + 0.217f * 7, 0.3f, -0.7595f + 0.217f * 7), Quaternion.identity);







    }
    
    //funktio siirron suorittamiselle (onko teleport siirto vai sulava siirto)
}
