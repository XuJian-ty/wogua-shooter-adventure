using System;
using UnityEngine;
using UnityEngine.UI;

namespace WoguaShooter.Update
{
    public class UpdatePanel : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button updateButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private Text updateButtonText;
        [SerializeField] private Text laterButtonText;

        public void Show(UpdateManifest manifest, Action updateAction, Action laterAction)
        {
            gameObject.SetActive(true);
            updateButton.gameObject.SetActive(true);
            titleText.text = string.IsNullOrWhiteSpace(manifest.title) ? "发现新版本" : manifest.title;
            messageText.text = $"版本 {manifest.version}\n{manifest.message}";
            statusText.text = string.Empty;
            updateButton.interactable = true;
            laterButton.interactable = true;
            updateButtonText.text = "立即更新";
            laterButtonText.text = "稍后更新";
            updateButton.onClick.RemoveAllListeners();
            laterButton.onClick.RemoveAllListeners();
            updateButton.onClick.AddListener(() => updateAction?.Invoke());
            laterButton.onClick.AddListener(() => laterAction?.Invoke());
        }

        public void SetDownloading()
        {
            statusText.text = "正在下载更新，请稍候...";
            updateButton.interactable = false;
            laterButton.interactable = false;
        }

        public void SetFailed(string message)
        {
            statusText.text = "更新失败：" + message;
            updateButton.interactable = true;
            laterButton.interactable = true;
        }

        public void SetComplete()
        {
            statusText.text = "更新完成，点击继续进入游戏。";
            updateButton.gameObject.SetActive(false);
            laterButton.interactable = true;
            laterButtonText.text = "继续游戏";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
