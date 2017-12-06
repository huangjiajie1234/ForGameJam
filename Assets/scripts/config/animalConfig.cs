using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animalConfig : MonoBehaviour {


    [SerializeField]
    private TextAsset config;
	// Use this for initialization

    public class animal
    {
        public int id;

        public int Type;

        public int FeedOn;

        public int Propagate;

        public int AntiDisease;

        public int Edible;

        public int Affair;
    }

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
