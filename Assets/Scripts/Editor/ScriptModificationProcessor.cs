using System;
using UnityEditor;
using UnityEngine;

namespace ChronoStasis{

    /*
     * 
     *  Template loacted in UnityLocation/Editor/Resources/ScriptTemplates
     * 
     * 
        using UnityEngine;

        namespace #NAMESPACE#{

	        public class #SCRIPTNAME# : MonoBehaviour {
	
	            #NOTRIM#
		
	        }
	
        }
     * 
     */

    public class ScriptModificationProcessor : UnityEditor.AssetModificationProcessor{

	    public static void OnWillCreateAsset( string path ){
	        path = path.Replace( ".meta", "" );
	        int index = path.LastIndexOf( ".", StringComparison.Ordinal );

	        if( index == -1 ) {
	            return;
	        }

	        string file = path.Substring( index );

	        if( file != ".cs" && file != ".js" && file != ".boo" ) return;

	        index = Application.dataPath.LastIndexOf( "Assets", StringComparison.Ordinal );
	        path = Application.dataPath.Substring( 0, index ) + path;
	        file = System.IO.File.ReadAllText( path );

	        file = file.Replace( "#NAMESPACE#", EditorSettings.projectGenerationRootNamespace );

	        System.IO.File.WriteAllText( path, file );
	        AssetDatabase.Refresh();
        }

    }
	
}
