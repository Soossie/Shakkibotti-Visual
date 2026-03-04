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
    
    //funktio siirron suorittamiselle (onko teleport siirto vai sulava siirto)
}
