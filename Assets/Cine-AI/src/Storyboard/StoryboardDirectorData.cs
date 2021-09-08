using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System.IO;

[System.Serializable]
public class CinematographyTechnique
{
    public CinematographyTechniqueImplementation m_implementation = null;
    public string m_title;
    public float m_value = 0.0f;
    public float m_dramatization = 0.0f;
    public float m_pace = 0.0f;
    public float m_classDistribution = 0.0f;
    public float m_probabilityDistribution = 0.0f;

    public void CalculateDistributions(float sum, float sumExp)
    {
        m_probabilityDistribution = m_value / sum;
        m_classDistribution = Mathf.Exp(m_value) / sumExp;
    }
}

[System.Serializable]
public class CinematographyTechniqueCategory
{
    public string m_title;
    public List<CinematographyTechnique> m_techniques = new List<CinematographyTechnique>();
    public string m_defaultTechniqueTitle;
    public CinematographyTechnique m_defaultTechnique = null;
    public bool m_foldout = false;
}


/// <summary>
/// Loaded via JSON utility in editor.
/// </summary>
[System.Serializable]
public class StoryboardDirectorData
{

    public List<CinematographyTechniqueCategory> m_categories = new List<CinematographyTechniqueCategory>();

    private int m_progressID = 0;

    public bool CalculateProbabilityAndClassDistribution(ref List<CinematographyTechniqueImplementation> implementations)
    {
        for (int i = 0; i < m_categories.Count; i++)
        {
            int defaultTechniqueIndex = m_categories[i].m_techniques.FindIndex(o => o.m_title.CompareTo(m_categories[i].m_defaultTechniqueTitle) == 0);

            if (defaultTechniqueIndex < 0)
            {
                Debug.LogError("Techniques inside the category does not contain the default technique, please check your JSON file!");
                return false;
            }

            m_categories[i].m_defaultTechnique = m_categories[i].m_techniques[defaultTechniqueIndex];

            m_categories[i].m_techniques.OrderBy(o => o.m_probabilityDistribution);

            float sum = m_categories[i].m_techniques.Sum(o => o.m_value);
            float sumExp = m_categories[i].m_techniques.Sum(o => Mathf.Exp(o.m_value));

            for (int j = 0; j < m_categories[i].m_techniques.Count; j++)
            {
                CinematographyTechnique technique = m_categories[i].m_techniques[j];
                technique.CalculateDistributions(sum, sumExp);

                // Check whether implementations exist.
                int implementationIndex = implementations.FindIndex(o => o.GetType().ToString().CompareTo(technique.m_title) == 0);
                if (implementationIndex < 0)
                {
                    Debug.LogError("Implementation for " + technique.m_title + " could not be found. Make sure you have an implementation class deriving from" +
                        "CinematographyTechniqueImplementation base class and is the same name as the technique's title.");
                    return false;
                }
                else
                    technique.m_implementation = implementations[implementationIndex];
            }
        }

        return true;
    }

    public void CreateImplementationDetails(string implementationResourcesPath, ref List<CinematographyTechniqueImplementation> implementations)
    {
        if (implementations == null)
            implementations = new List<CinematographyTechniqueImplementation>();

        System.Type[] types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
        System.Type[] derivedTechniques = (from System.Type type in types where type.IsSubclassOf(typeof(CinematographyTechniqueImplementation)) select type).ToArray();

        if (Progress.Exists(m_progressID))
            Progress.Cancel(m_progressID);

        m_progressID = Progress.Start("Cinematography Techniques", "Creating assets from derived cinematography techniques.");
        for (int i = 0; i < derivedTechniques.Length; i++)
        {
            System.Type type = derivedTechniques[i];
            string assetName = implementationResourcesPath + "/" + type.ToString() + ".asset";
            bool fileExists = File.Exists(assetName);
            bool implementationContains = false;

            if (fileExists)
                implementationContains = implementations.FindIndex(o => o != null && o.GetType().ToString().CompareTo(type.ToString()) == 0) >= 0;

            if (!fileExists || !implementationContains)
            {
                if (!AssetDatabase.IsValidFolder(implementationResourcesPath))
                {
                    Debug.LogError("Implementation Resources Path is not valid. " + implementationResourcesPath);
                    return;
                }

                ScriptableObject instance = ScriptableObject.CreateInstance(type.ToString());

                if(instance != null)
                {
                    AssetDatabase.CreateAsset(instance, assetName);
                    AssetDatabase.SaveAssets();
                    implementations.Add((CinematographyTechniqueImplementation)instance);
                }
                else
                {
                    Debug.LogError("Asset instance is null. This usually means that your derived class does not have a script.cs file with it's own name.");
                }

            }
            else
            {
                
            }

            Progress.Report(m_progressID, (float)i / (float)derivedTechniques.Length);
        }

        for(int i= 0; i < implementations.Count; i++)
        {
            if (implementations[i] == null)
                implementations.RemoveAt(i);
        }

        Progress.Finish(m_progressID);

    }
}
