using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EButton : Attribute
{
    public string text;

    public EButton()
    {

    }

    public EButton(string text)
    {
        this.text = text;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BeginHorizontal : Attribute
    {
        public string text;

        public BeginHorizontal()
        {

        }

        public BeginHorizontal(string text)
        {
            this.text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EndHorizontal : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BeginVertical : Attribute
    {
        public string text;

        public BeginVertical()
        {

        }

        public BeginVertical(string text)
        {
            this.text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EndVertical : Attribute
    {

    }
}