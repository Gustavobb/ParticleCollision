using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example_EButton : MonoBehaviour {

    [EButton]
    void Do()
    {

    }

    [EButton.BeginHorizontal, EButton.BeginVertical("A"), EButton]
    void A1()
    {

    }

    [EButton]
    void A2()
    {

    }

    [EButton, EButton.EndVertical]
    void A3()
    {

    }

    
    [EButton.BeginVertical("B"), EButton]
    void B1()
    {

    }

    [EButton]
    void B2()
    {

    }


    [EButton()]
    void B3()
    {

    }
}