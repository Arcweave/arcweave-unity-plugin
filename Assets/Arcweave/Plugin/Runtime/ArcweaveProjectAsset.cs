﻿using UnityEngine;
using UnityEngine.Networking;

namespace Arcweave
{
    ///<summary>An arcweave project wrapper stored as a ScriptableObject asset</summary>
    [CreateAssetMenu(menuName = "Arcweave/Project Asset")]
    public class ArcweaveProjectAsset : ScriptableObject
    {
        public enum ImportSource { FromJson, FromWeb, }

        public ImportSource importSource;
        public TextAsset projectJsonFile;
        public string userAPIKey;
        public string projectHash;

        [field: SerializeField, HideInInspector]
        public Project project { get; private set; }
        public bool isImporting { get; private set; }

        ///----------------------------------------------------------------------------------------------

        [ContextMenu("Clear Data")]
        void ClearData() => project = null;

        //...
        protected void OnEnable() {
            if ( project != null ) {
                project.Initialize();
            }
        }

        ///<summary>Import project from json text file or web and get callback when finished.</summary>
        public void ImportProject(System.Action callback = null) {
            if ( importSource == ImportSource.FromJson && projectJsonFile != null ) {
                MakeProject(projectJsonFile.text, callback);
            }
            if ( importSource == ImportSource.FromWeb && !string.IsNullOrEmpty(userAPIKey) ) {
                SendWebRequest((j) => MakeProject(j, callback));
            }
        }

        //...
        async void MakeProject(string json, System.Action callback) {
            ProjectMaker maker = null;
            await System.Threading.Tasks.Task.Run(() =>
            {
                Debug.Log("Parsing Json...");
                maker = new ProjectMaker(json, this);
                Debug.Log("Making Project...");
                project = maker.MakeProject();
            });

            Debug.Log("Making ArcscriptImplementations C# file...");
            maker.MakeArcscriptFile(this);
            Debug.Log("Done");
            if ( callback != null ) { callback(); }
        }

        //...
        void SendWebRequest(System.Action<string> callbackSuccess) {
            Debug.Log("Sending Web Request...");
            var requestUrl = string.Format("https://arcweave.com/api/{0}/unity", projectHash);
            var request = UnityWebRequest.Get(requestUrl);
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", userAPIKey));
            request.SetRequestHeader("Accept", "application/json");
            var requestOperation = request.SendWebRequest();
            requestOperation.completed += (op) =>
            {
                var responseCode = request.responseCode;
                Debug.Log(string.Format("Web Request Completed (code = {0})...", responseCode));
                var result = request.downloadHandler?.text;
                if ( responseCode == 200 && callbackSuccess != null ) {
                    callbackSuccess(result);
                }
                request.Dispose();
            };
        }
    }
}