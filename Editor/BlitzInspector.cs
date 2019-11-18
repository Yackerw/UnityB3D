using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BlitzInspector : ShaderGUI
{
	private void AddButton(string buttonText, string keyWord, Material mat)
	{
		// check if additive tex is on...
		bool toggle = mat.IsKeywordEnabled(keyWord);
		EditorGUI.BeginChangeCheck();
		// draw the checkbox...
		toggle = EditorGUILayout.Toggle(buttonText, toggle);
		// update material to match checkbox
		if (EditorGUI.EndChangeCheck())
		{
			// if it's enabled in the editor, then we need to set/disable the keyword in the material
			if (toggle)
			{
				mat.EnableKeyword(keyWord);
			}
			else
			{
				mat.DisableKeyword(keyWord);
			}
			EditorUtility.SetDirty(mat);
		}
	}

	public override void OnGUI(MaterialEditor me, MaterialProperty[] properties)
	{
		// default inspector
		base.OnGUI(me, properties);

		// not visible, return
		if (!me.isVisible)
			return;

		// get the material...
		Material mat = (Material)me.target;

		AddButton("Additive Texture on", "ADDTEX_ON", mat);
		AddButton("Multiplicative Texture on", "MULTEX_ON", mat);
		AddButton("Multiplicative Texture 2 on", "MULTEX2_ON", mat);
		AddButton("Additive Sphere Map on", "ADDSPHERE_ON", mat);
		AddButton("Multiplicative Sphere Map on", "MULSPHERE_ON", mat);
		AddButton("Specular on", "SPEC_ON", mat);
	}
}