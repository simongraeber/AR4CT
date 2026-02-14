using UnityEngine;

namespace CT4AR.UI
{
    /// <summary>
    /// Opens a URL in the device browser. Wire a Button's OnClick to OpenURL().
    /// </summary>
    public class OpenBrowser : MonoBehaviour
    {
        [SerializeField] private string url = "https://ar4ct.com";

        public void SetURL(string newUrl)
        {
            url = newUrl;
        }

        public void OpenURL()
        {
            Application.OpenURL(url);
        }
    }
}
