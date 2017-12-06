using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class countryConfig : MonoBehaviour
{

    public class country
    {
        public int id;

        public int name;

        public int Type;

        public int Clean;

        public int Hunger;

        public int AnimalProtect;

        public int Environment;

        public Dictionary<int, int> animals;

    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
