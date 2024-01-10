using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/*  The whole purpose of this is to allow for different fall off types to be sellected. */

[CustomEditor(typeof(Projectile_Controller))] public class Projectile_Controller_Editor : Editor{
    
    public override void OnInspectorGUI(){
        serializedObject.Update();

        // Show all non-specific properties:
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Lifetime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Damage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sfx_cast"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sfx_Hit"));
        
        // Show lighting options:
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lighting"));
        Projectile_Controller.Lighting lightingOption;
        lightingOption = (Projectile_Controller.Lighting)serializedObject.FindProperty("lighting").enumValueIndex;
        
        switch(lightingOption){
            case Projectile_Controller.Lighting.lighting1:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Intensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Range"));
                break;
            
            case Projectile_Controller.Lighting.lighting2:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Intensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Range"));
                break;
            
            case Projectile_Controller.Lighting.lighting3:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Intensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Caster_Range"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Intensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Projectile_Range"));
                break;
        }


        // Find the falloff option selected:
        EditorGUILayout.PropertyField(serializedObject.FindProperty("falloff"));
        Projectile_Controller.FalloffOptions falloffOption;
        falloffOption = (Projectile_Controller.FalloffOptions)serializedObject.FindProperty("falloff").enumValueIndex;

        // Switch case to show or hide options. I have them all writen out just in case i change the functionality.
        switch (falloffOption){
            case Projectile_Controller.FalloffOptions.Linear:       // no parameters for Linear.
            break;    
            
            case Projectile_Controller.FalloffOptions.Quadratic:    // no parameters for Quadratic.
            break;    
            
            case Projectile_Controller.FalloffOptions.Cosine:       // no parameters for Cosine.
            break;    
            
            case Projectile_Controller.FalloffOptions.Exponent:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Exponent"));
            break;
            
            case Projectile_Controller.FalloffOptions.ExponentCosine:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Exponent"));
            break;
            
            case Projectile_Controller.FalloffOptions.Boomerang:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Smoothness"));
            break;
        }

        // Show non-specific falloff settings:
        EditorGUILayout.PropertyField(serializedObject.FindProperty("falloffDirection"));

        // Apply the changes made to the projectile controller.
        serializedObject.ApplyModifiedProperties();
    }
}