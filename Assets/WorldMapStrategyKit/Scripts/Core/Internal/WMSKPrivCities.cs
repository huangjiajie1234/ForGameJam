// World Political Map - Globe Edition for Unity - Main Script
// Copyright 2015 Kronnect Games
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WPM

//#define TRACE_CTL

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit {

	public partial class WMSK : MonoBehaviour {

		const float CITY_HIT_PRECISION = 0.00075f;
		const string CITY_ATTRIB_DEFAULT_FILENAME = "citiesAttrib";

		// resources & gameobjects
		Material citiesNormalMat, citiesRegionCapitalMat, citiesCountryCapitalMat;
		GameObject citiesLayer;

		// internal cache
		City[] visibleCities;

		/// <summary>
		/// City look up dictionary. Used internally for fast searching of city objects.
		/// </summary>
		Dictionary<City, int>_cityLookup;
		int lastCityLookupCount = -1;
		
		Dictionary<City, int>cityLookup {
			get {
				if (_cityLookup != null && cities.Count == lastCityLookupCount)
					return _cityLookup;
				if (_cityLookup == null) {
					_cityLookup = new Dictionary<City,int> ();
				} else {
					_cityLookup.Clear ();
				}
				if (cities!=null) {
					int cityCount = cities.Count;
					for (int k=0; k<cityCount; k++)
						_cityLookup.Add (_cities [k], k);
				}
				lastCityLookupCount = _cityLookup.Count;
				return _cityLookup;
			}
		}

		#region IO stuff

		void ReadCitiesPackedString () {
			string cityCatalogFileName = _geodataResourcesPath + "/cities10";
			TextAsset ta = Resources.Load<TextAsset> (cityCatalogFileName);
			string s = ta.text;

			lastCityLookupCount = -1;

			string[] cityList = s.Split (new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			int cityCount = cityList.Length;
			_cities = new List<City> (cityCount);
			for (int k=0; k<cityCount; k++) {
				string[] cityInfo = cityList [k].Split (new char[] { '$' });
				string country = cityInfo [2];
				int countryIndex = GetCountryIndex(country);
				if (countryIndex>=0) {
					string name = cityInfo [0];
					string province = cityInfo [1];
					int population = int.Parse (cityInfo [3]);
					float x = float.Parse (cityInfo [4]);
					float y = float.Parse (cityInfo [5]);
					CITY_CLASS cityClass = (CITY_CLASS)int.Parse(cityInfo[6]);
					int uniqueId;
					if (cityInfo.Length>=8) {
						uniqueId = int.Parse(cityInfo[7]);
					} else {
						uniqueId = GetUniqueId( new List<IExtendableAttribute>(_cities.ToArray()));
					}					
					City city = new City (name, province, countryIndex, population, new Vector3 (x, y), cityClass, uniqueId);
					_cities.Add (city);
				}
			}
			ReloadCitiesAttributes();
		}


		void ReloadCitiesAttributes() {
			TextAsset ta = Resources.Load<TextAsset> (_geodataResourcesPath + "/" + _cityAttributeFile);
			if (ta==null) return;
			SetCitiesXMLAttributes(ta.text);
		}

		#endregion


	#region Drawing stuff

		void CheckCityIcons() {
			if (_citySpot == null) _citySpot = Resources.Load <GameObject> ("WMSK/Prefabs/CitySpot");
			if (_citySpotCapitalRegion == null) _citySpotCapitalRegion = Resources.Load <GameObject> ("WMSK/Prefabs/CityCapitalRegionSpot");
			if (_citySpotCapitalCountry == null) _citySpotCapitalCountry = Resources.Load <GameObject> ("WMSK/Prefabs/CityCapitalCountrySpot");
		}


		/// <summary>
		/// Redraws the cities. This is automatically called by Redraw(). Used internally by the Map Editor. You should not need to call this method directly.
		/// </summary>
		public void DrawCities () {

			if (!_showCities || !gameObject.activeInHierarchy) return;

			CheckCityIcons();

			// Create cities layer
			Transform t = transform.Find ("Cities");
			if (t != null)
				DestroyImmediate (t.gameObject);
			citiesLayer = new GameObject ("Cities");
			citiesLayer.hideFlags = HideFlags.DontSave;
			citiesLayer.transform.SetParent (transform, false);
			citiesLayer.transform.localPosition = Misc.Vector3back * 0.001f;
			citiesLayer.layer = gameObject.layer;

			// Create cityclass parents
			GameObject countryCapitals = new GameObject("Country Capitals");
			countryCapitals.hideFlags = HideFlags.DontSave;
			countryCapitals.transform.SetParent(citiesLayer.transform, false);
			GameObject regionCapitals = new GameObject("Region Capitals");
			regionCapitals.hideFlags = HideFlags.DontSave;
			regionCapitals.transform.SetParent(citiesLayer.transform, false);
			GameObject normalCities = new GameObject("Normal Cities");
			normalCities.hideFlags = HideFlags.DontSave;
			normalCities.transform.SetParent(citiesLayer.transform, false);

			if (cities==null) return;
			// Draw city marks
			numCitiesDrawn = 0;
			int minPopulation = _minPopulation * 1000;
			int visibleCount = 0;

			int cityCount = cities.Count;
			for (int k=0; k<cityCount; k++) {
				City city = _cities [k];
				Country country = countries[city.countryIndex];
				city.isVisible = !country.hidden && ( (((int)city.cityClass & _cityClassAlwaysShow) != 0) || (minPopulation==0 || city.population >= minPopulation) );
				if (city.isVisible) {
					GameObject cityObj, cityParent;
					Material mat;
					switch(city.cityClass) {
					case CITY_CLASS.COUNTRY_CAPITAL: 
						cityObj = Instantiate (_citySpotCapitalCountry); 
						mat = citiesCountryCapitalMat;
						cityParent = countryCapitals;
						break;
					case CITY_CLASS.REGION_CAPITAL: 
						cityObj = Instantiate (_citySpotCapitalRegion); 
						mat = citiesRegionCapitalMat;
						cityParent = regionCapitals;
						break;
					default:
						cityObj = Instantiate (_citySpot); 
						mat = citiesNormalMat;
						cityParent = normalCities;
						break;
					}
					cityObj.name = k.ToString();
					cityObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
					cityObj.layer = citiesLayer.layer;
					cityObj.transform.SetParent (cityParent.transform, false);
					cityObj.transform.localPosition = city.unity2DLocation;
					Renderer rr = cityObj.GetComponent<Renderer>();
					if (rr!=null) rr.sharedMaterial = mat;

					numCitiesDrawn++;
					visibleCount++;
				}
			}

			// Cache visible cities (this is faster than iterate through the entire collection)
			if (visibleCities==null || visibleCities.Length!=visibleCount)  {
				visibleCities = new City[visibleCount];
			}
			for (int k=0;k<cityCount;k++) {
				City city = _cities[k];
				if (city.isVisible) visibleCities[--visibleCount] = city;
			}

			// Toggle cities layer visibility according to settings
			citiesLayer.SetActive (_showCities);
			ScaleCities();
		}

		public string GetCityHierarchyName(int cityIndex) {
			if (cityIndex<0 || cityIndex>=cities.Count) return "";
			switch(cities[cityIndex].cityClass) {
			case CITY_CLASS.COUNTRY_CAPITAL: return "Cities/Country Capitals/" + cityIndex.ToString();
			case CITY_CLASS.REGION_CAPITAL: return "Cities/Region Capitals/" + cityIndex.ToString();
			default: return "Cities/Normal Cities/" + cityIndex.ToString();
			}
		}

		void ScaleCities() {
			if (!gameObject.activeInHierarchy) return;
			CityScaler cityScaler = citiesLayer.GetComponent<CityScaler>() ?? citiesLayer.AddComponent<CityScaler>();
			cityScaler.map = this;
			if (_showCities) {
				cityScaler.ScaleCities();
			}
		}

		void HighlightCity(int cityIndex) {
			_cityHighlightedIndex = cityIndex;
			_cityHighlighted = cities[cityIndex];
			
			// Raise event
			if (OnCityEnter!=null) OnCityEnter(_cityHighlightedIndex);
		}
		
		void HideCityHighlight() {
			if (_cityHighlightedIndex<0) return;
			
			// Raise event
			if (OnCityExit!=null) OnCityExit(_cityHighlightedIndex);
			_cityHighlighted = null;
			_cityHighlightedIndex = -1;
		}


	#endregion

		#region Internal API

		/// <summary>
		/// Returns any city near the point specified in local coordinates.
		/// </summary>
		int GetCityNearPoint(Vector3 localPoint) {
			return GetCityNearPoint(localPoint, -1);
		}

		/// <summary>
		/// Returns any city near the point specified for a given country in local coordinates.
		/// </summary>
		int GetCityNearPoint(Vector3 localPoint, int countryIndex) {
			if (visibleCities==null) return -1;
			float rl = localPoint.x - CITY_HIT_PRECISION;
			float rr = localPoint.x + CITY_HIT_PRECISION;
			float rt = localPoint.y + CITY_HIT_PRECISION;
			float rb = localPoint.y - CITY_HIT_PRECISION;
			for (int c=0;c<visibleCities.Length;c++) {
				City city = visibleCities[c];
				if (countryIndex<0 || city.countryIndex == countryIndex) {
					Vector2 cityLoc = city.unity2DLocation;
					if (cityLoc.x>rl && cityLoc.x<rr && cityLoc.y>rb && cityLoc.y<rt) {
						return GetCityIndex (city);
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the file name corresponding to the current city data file
		/// </summary>
		public string GetCityFileName() {
			return "cities10.txt";
		}


		#endregion
		
		
	}
	
}