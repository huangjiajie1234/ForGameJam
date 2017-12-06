// World Strategy Kit for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{
	public delegate void OnCityEnter(int cityIndex);
	public delegate void OnCityExit(int cityIndex);
	public delegate void OnCityClick(int cityIndex);

	public partial class WMSK : MonoBehaviour
	{

		#region Public properties

		List<City> _cities;

		/// <summary>
		/// Complete list of cities with their names and country names.
		/// </summary>
		public List<City> cities {
			get { 
				if (_cities==null) ReadCitiesPackedString();
				return _cities;
			}
			set { _cities = value; lastCityLookupCount = -1; }
		}

		public const int CITY_CLASS_FILTER_REGION_CAPITAL_CITY = 2;
		public const int CITY_CLASS_FILTER_COUNTRY_CAPITAL_CITY = 4;

		/// <summary>
		/// Returns City under mouse position or null if none.
		/// </summary>
		City _cityHighlighted;
		/// <summary>
		/// Returns City under mouse position or null if none.
		/// </summary>
		public City cityHighlighted { get { return _cityHighlighted; } }
		
		int _cityHighlightedIndex = -1;
		/// <summary>
		/// Returns City index mouse position or null if none.
		/// </summary>
		public int cityHighlightedIndex { get { return _cityHighlightedIndex; } }
		
		int _cityLastClicked = -1;
		/// <summary>
		/// Returns the last clicked city index.
		/// </summary>
		public int cityLastClicked { get { return _cityLastClicked; } }
	
		public event OnCityEnter OnCityEnter;
		public event OnCityEnter OnCityExit;
		public event OnCityClick OnCityClick;

		[SerializeField]
		bool
			_showCities = true;

		/// <summary>
		/// Toggle cities visibility.
		/// </summary>
		public bool showCities { 
			get {
				return _showCities; 
			}
			set {
				if (_showCities != value) {
					_showCities = value;
					isDirty = true;
					if (citiesLayer != null) {
						citiesLayer.SetActive (_showCities);
					} else if (_showCities) {
						DrawCities ();
					}
				}
			}
		}

		[SerializeField]
		Color
			_citiesColor = Color.white;
		
		/// <summary>
		/// Global color for cities.
		/// </summary>
		public Color citiesColor {
			get {
				if (citiesNormalMat != null) {
					return citiesNormalMat.color;
				} else {
					return _citiesColor;
				}
			}
			set {
				if (value != _citiesColor) {
					_citiesColor = value;
					isDirty = true;
					
					if (citiesNormalMat != null && _citiesColor != citiesNormalMat.color) {
						citiesNormalMat.color = _citiesColor;
					}
				}
			}
		}
		
		[SerializeField]
		Color
			_citiesRegionCapitalColor = Color.cyan;
		
		/// <summary>
		/// Global color for region capitals.
		/// </summary>
		public Color citiesRegionCapitalColor {
			get {
				if (citiesRegionCapitalMat != null) {
					return citiesRegionCapitalMat.color;
				} else {
					return _citiesRegionCapitalColor;
				}
			}
			set {
				if (value != _citiesRegionCapitalColor) {
					_citiesRegionCapitalColor = value;
					isDirty = true;
					
					if (citiesRegionCapitalMat != null && _citiesRegionCapitalColor != citiesRegionCapitalMat.color) {
						citiesRegionCapitalMat.color = _citiesRegionCapitalColor;
					}
				}
			}
		}
		
		
		[SerializeField]
		Color
			_citiesCountryCapitalColor = Color.yellow;
		
		/// <summary>
		/// Global color for country capitals.
		/// </summary>
		public Color citiesCountryCapitalColor {
			get {
				if (citiesCountryCapitalMat != null) {
					return citiesCountryCapitalMat.color;
				} else {
					return _citiesCountryCapitalColor;
				}
			}
			set {
				if (value != _citiesCountryCapitalColor) {
					_citiesCountryCapitalColor = value;
					isDirty = true;
					
					if (citiesCountryCapitalMat != null && _citiesCountryCapitalColor != citiesCountryCapitalMat.color) {
						citiesCountryCapitalMat.color = _citiesCountryCapitalColor;
					}
				}
			}
		}		

		[SerializeField]
		float _cityIconSize = 1.0f;

		/// <summary>
		/// The size of the cities icon (dot).
		/// </summary>
		public float cityIconSize { 
			get {
				return _cityIconSize; 
			}
			set {
				if (value != _cityIconSize) {
					_cityIconSize = value;
					ScaleCities (); 
					ScaleMountPoints();	// for the Editor's icon: mount points are invisible at runtime
					isDirty = true;
				}
			}
		}

		[SerializeField]
		GameObject _citySpot;
		/// <summary>
		/// Allows you to change default icon for normal cities. This must be a 2D game object (you may duplicate and modify city prefabs in WMSK/Resources/Prefabs folder).
		/// </summary>
		public GameObject citySpot {
			get {
				return _citySpot;
			}
			set {
				if (value != _citySpot) {
					_citySpot = value;
					isDirty = true;
					DrawCities();
				}
			}
		}

		[SerializeField]
		GameObject _citySpotCapitalRegion;
		/// <summary>
		/// Allows you to change default icon for region capitals. This must be a 2D game object (you may duplicate and modify city prefabs in WMSK/Resources/Prefabs folder).
		/// </summary>
		public GameObject citySpotCapitalRegion {
			get {
				return _citySpotCapitalRegion;
			}
			set {
				if (value != _citySpotCapitalRegion) {
					_citySpotCapitalRegion = value;
					isDirty = true;
					DrawCities();
				}
			}
		}

		
		[SerializeField]
		GameObject _citySpotCapitalCountry;
		/// <summary>
		/// Allows you to change default icon for country capitals. This must be a 2D game object (you may duplicate and modify city prefabs in WMSK/Resources/Prefabs folder).
		/// </summary>
		public GameObject citySpotCapitalCountry {
			get {
				return _citySpotCapitalCountry;
			}
			set {
				if (value != _citySpotCapitalCountry) {
					_citySpotCapitalCountry = value;
					isDirty = true;
					DrawCities();
				}
			}
		}


		[Range(0, 17000)]
		[SerializeField]
		int
			_minPopulation = 0;

		public int minPopulation {
			get {
				return _minPopulation;
			}
			set {
				if (value != _minPopulation) {
					_minPopulation = value;
					isDirty = true;
					DrawCities ();
				}
			}
		}

		
		[SerializeField]
		int _cityClassAlwaysShow;

		/// <summary>
		/// Flags for specifying the class of cities to always show irrespective of other filters like minimum population. Can assign a combination of bit flags defined by CITY_CLASS_FILTER* 
		/// </summary>
		public int cityClassAlwaysShow {
			get { return _cityClassAlwaysShow; }
			set { if (_cityClassAlwaysShow!=value) {
					_cityClassAlwaysShow = value;
					isDirty = true;
					DrawCities();
				}
			}
		}

		[NonSerialized]
		public int
			numCitiesDrawn = 0;


		string _cityAttributeFile = CITY_ATTRIB_DEFAULT_FILENAME;
		
		public string cityAttributeFile {
			get { return _cityAttributeFile; }
			set { if (value!=_cityAttributeFile) {
					_cityAttributeFile = value;
					if (_cityAttributeFile == null) _cityAttributeFile = CITY_ATTRIB_DEFAULT_FILENAME;
					isDirty = true;
					ReloadCitiesAttributes();
				}
			}
		}

	#endregion

	#region Public API area

		/// <summary>
		/// Deletes all cities of current selected country's continent
		/// </summary>
		public void CitiesDeleteFromContinent(string continentName) {
			HideCityHighlights();
			int k=-1;
			int cityCount = cities.Count;
			while(++k<cityCount) {
				int cindex = _cities[k].countryIndex;
				if (cindex>=0) {
					string cityContinent = countries[cindex].continent;
					if (cityContinent.Equals(continentName)) {
						_cities.RemoveAt(k);
						k--;
					}
				}
			}
		}

		/// <summary>
		/// Returns the index of a city in the global cities collection. Note that country name needs to be supplied due to repeated city names.
		/// </summary>
		public int GetCityIndex (string cityName, string countryName)
		{
			int countryIndex = GetCountryIndex(countryName);
			return GetCityIndex(cityName, countryIndex);
		}

		/// <summary>
		/// Returns the index of a city in the global cities collection. Note that country index needs to be supplied due to repeated city names.
		/// </summary>
		public int GetCityIndex (string cityName, int countryIndex)
		{
			int cityCount = cities.Count;
			if (countryIndex >= 0 && countryIndex < countries.Length) {
				for (int k=0; k<cityCount; k++) {
					if (_cities [k].countryIndex == countryIndex && _cities [k].name.Equals (cityName))
						return k;
				}
			} else {
				// Try to select city by its name alone
				for (int k=0; k<cityCount; k++) {
					if (_cities [k].name.Equals (cityName))
						return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Flashes specified city by index in the global city collection.
		/// </summary>
		public void BlinkCity (int cityIndex, Color color1, Color color2, float duration, float blinkingSpeed)
		{
			if (citiesLayer == null)
				return;

			string cobj = GetCityHierarchyName(cityIndex);
			Transform t = transform.Find (cobj);
			if (t == null)
				return;
			if (t.GetComponent<Renderer>()==null) {
				Debug.Log ("City game object needs a renderer component for blinking effect.");
				return;
			}
			CityBlinker sb = t.gameObject.AddComponent<CityBlinker> ();
			sb.blinkMaterial = t.GetComponent<Renderer>().sharedMaterial;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
		}


		/// <summary>
		/// Starts navigation to target city. Returns false if not found.
		/// </summary>
		public bool FlyToCity (string cityName, string countryName)
		{
			return FlyToCity (name, countryName, _navigationTime);
		}

		/// <summary>
		/// Starts navigation to target city with duration provided. Returns false if not found.
		/// </summary>
		public bool FlyToCity (string name, string countryName, float duration)
		{
			int cityIndex = GetCityIndex(name, countryName);
			if (cityIndex>=0) {
				FlyToCity (cityIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target city by index in the cities collection.
		/// </summary>
		public void FlyToCity (int cityIndex)
		{
			FlyToCity (cityIndex, _navigationTime);
		}

		
		/// <summary>
		/// Starts navigation to target city by index with duration providedn
		/// </summary>
		public void FlyToCity (int cityIndex, float duration)
		{
			SetDestination (cities [cityIndex].unity2DLocation, duration);
		}

		/// <summary>
		/// Returns the index of a city in the cities collection by its reference.
		/// </summary>
		public int GetCityIndex (City city)
		{
			if (cityLookup.ContainsKey (city)) 
				return cityLookup [city];
			else
				return -1;
		}

		/// <summary>
		/// Returns the city object by its name belonging to provided country name.
		/// </summary>
		public City GetCity (string cityName, string countryName)
		{
			int countryIndex = GetCountryIndex(countryName);
			if (countryIndex<0) return null;
			int cityIndex = GetCityIndex(cityName, countryIndex);
			if (cityIndex>=0) return cities[cityIndex];
			return null;
		}

		/// <summary>
		/// Gets the city index with that unique Id.
		/// </summary>
		public int GetCityIndex(int uniqueId) {
			int cityCount = cities.Count;
			for (int k=0;k<cityCount;k++) {
				if (cities[k].uniqueId==uniqueId) return k;
			}
			return -1;
		}


		/// <summary>
		/// Returns the city index by screen position.
		/// </summary>
		public bool GetCityIndex (Ray ray, out int cityIndex)
		{
			RaycastHit[] hits = Physics.RaycastAll (ray, 500, layerMask);
			if (hits.Length > 0) {
				for (int k=0; k<hits.Length; k++) {
					if (hits [k].collider.gameObject == gameObject) {
						Vector3 localHit = transform.InverseTransformPoint (hits [k].point);
						int c = GetCityNearPoint (localHit);
						if (c >= 0) {
							cityIndex = c;
							return true;
						}
					}
				}
			}
			cityIndex = -1;
			return false;
		}


		/// <summary>
		/// Clears any city highlighted (color changed) and resets them to default city color
		/// </summary>
		public void HideCityHighlights ()
		{
			DrawCities ();
		}


		/// <summary>
		/// Toggles the city highlight.
		/// </summary>
		/// <param name="cityIndex">City index.</param>
		/// <param name="color">Color.</param>
		/// <param name="highlighted">If set to <c>true</c> the color of the city will be changed. If set to <c>false</c> the color of the city will be reseted to default color</param>
		public void ToggleCityHighlight (int cityIndex, Color color, bool highlighted)
		{
			if (citiesLayer == null)
				return;
			string cobj = GetCityHierarchyName(cityIndex);
			Transform t = transform.Find (cobj);
			if (t == null)
				return;
			Renderer rr = t.gameObject.GetComponent<Renderer> ();
			if (rr == null)
				return;
			Material mat;
			if (highlighted) {
				mat = Instantiate (rr.sharedMaterial);
				mat.hideFlags = HideFlags.DontSave;
				mat.color = color;
				rr.sharedMaterial = mat;
			} else {
				switch(cities[cityIndex].cityClass) {
				case CITY_CLASS.COUNTRY_CAPITAL: mat = citiesCountryCapitalMat; break;
				case CITY_CLASS.REGION_CAPITAL: mat = citiesRegionCapitalMat; break;
				default: mat = citiesNormalMat; break;
				}
				rr.sharedMaterial = mat;
			}
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames ()
		{
			int cityCount = cities.Count;
			List<string> c = new List<string> (cityCount);
			for (int k=0; k<cityCount; k++) {
				c.Add (_cities [k].name + " (" + k + ")");
			}
			c.Sort ();
			return c.ToArray ();
		}

		/// <summary>
		/// Returns an array with the city names.
		/// </summary>
		public string[] GetCityNames (int countryIndex) {
			int cityCount = cities.Count;
			List<string> c = new List<string> (cityCount);
			for (int k=0; k<cityCount; k++) {
				if (_cities[k].countryIndex == countryIndex) {
					c.Add (_cities [k].name + " (" + k + ")");
				}
			}
			c.Sort ();
			return c.ToArray ();
		}


		/// <summary>
		/// Returns a list of cities whose attributes matches predicate
		/// </summary>
		public List<City> GetCities(  attribPredicate predicate ) {
			List <City> selectedCities = new List<City>();
			int cityCount = cities.Count;
			for (int k=0;k<cityCount;k++) {
				City city = _cities[k];
				if (predicate(city.attrib)) selectedCities.Add (city);
			}
			return selectedCities;
		}
		
		/// <summary>
		/// Gets XML attributes of all cities in jSON format.
		/// </summary>
		public string GetCitiesXMLAttributes(bool prettyPrint) {
			return GetCitiesXMLAttributes (new List<City>(cities), prettyPrint);
		}
		
		/// <summary>
		/// Gets XML attributes of provided cities in jSON format.
		/// </summary>
		public string GetCitiesXMLAttributes(List<City> cities, bool prettyPrint) {
			JSONObject composed = new JSONObject();
			int cityCount = cities.Count;
			for (int k=0;k<cityCount;k++) {
				City city = _cities[k];
				if (city.attrib.keys!=null) composed.AddField(city.uniqueId.ToString(), city.attrib);
			}
			return composed.Print(prettyPrint);
		}
		
		/// <summary>
		/// Sets cities attributes from a jSON formatted string.
		/// </summary>
		public void SetCitiesXMLAttributes(string jSON) {
			JSONObject composed = new JSONObject(jSON);
			int keyCount = composed.keys.Count;
			for (int k=0;k<keyCount;k++) {
				int uniqueId = int.Parse (composed.keys[k]);
				int cityIndex = GetCityIndex(uniqueId);
				if (cityIndex>=0) {
					cities[cityIndex].attrib = composed[k];
				}
			}
		}

		#endregion

	}

}