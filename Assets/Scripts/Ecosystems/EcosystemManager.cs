using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class EcosystemManager : Singleton<EcosystemManager> {

	[SerializeField]
	private GameObject regionPrefab;
	[SerializeField]
	private TextAsset regionJson;
	[SerializeField]
	private Transform regionParent;

}

    

