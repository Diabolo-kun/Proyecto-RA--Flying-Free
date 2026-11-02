using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyingFree.Trailer
{
    public sealed class TrailerMenuLink : MonoBehaviour
    {
        public string trailerScene = "TrailerAR";
        bool loading;
        public void OpenTrailer()
        {
            if (loading) return;
            loading = true;
            var operation = SceneManager.LoadSceneAsync(trailerScene);
            if (operation == null) loading = false;
        }
    }
}
