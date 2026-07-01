using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
  [SerializeField] private SlotManager slotManager;
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private BonusManager bonusManager;
  [SerializeField] private PopupManager popupManager;

  [System.Serializable]
  public struct SymbolTextMap
  {
      [SerializeField] internal int symbolId;
      [SerializeField] internal TMP_Text textComponent;
  }

  [Header("Info Panel UI")]
  [SerializeField] private GameObject infoPanel;
  [SerializeField] private Button infoButton;
  [SerializeField] private Button infoBackButton;
  [SerializeField] private List<SymbolTextMap> symbolTextMaps;

  [Header("Spin & Autoplay Rework UI")]
  [SerializeField] private GameObject autoplaySelectionPanel;
  [SerializeField] private TMP_Dropdown autoplayOptionsDropdown;
  [SerializeField] private Button autoplayStartButton;
  [SerializeField] private Button autoplayPanelClose;
  [SerializeField] private GameObject autoplayCounterObject;
  [SerializeField] private TMP_Text autoplayCounterText;

  [Header("Jackpot UI")]
  [SerializeField] private List<TMP_Text> JackpotText;

  [Header("RaycastBlocker")]
  [SerializeField] internal GameObject RaycastBlocker;

  [Header("Feature Panels")]
  [SerializeField] private GameObject featureWinPanel;
  [SerializeField] private GameObject spinCounterPanel;
  [SerializeField] private TMP_Text featureWinText;
  [SerializeField] private TMP_Text spinCounterText;

  [Header("Feature Popup UI")]
  [SerializeField] private GameObject featurePopup;
  [SerializeField] private Button featureStartButton;
  [SerializeField] private GameObject featureTitleObject;
  [SerializeField] private GameObject featureWinObject;
  [SerializeField] private TMP_Text featureWinAmountText;

  [Header("Walter Stash Grand Prize Popup UI")]
  [SerializeField] private GameObject walterStashPopup;
  [SerializeField] private TMP_Text walterStashAmountText;

  [Header("Win Type Popup UI")]
  [SerializeField] internal WinTypePopup winTypePopup;

  [Header("Feature Controls")]
  [SerializeField] private Button featureSpinButton;

  [Header("Free Spin Transition UI")]
  [SerializeField] private CanvasGroup normalBgCanvasGroup;
  [SerializeField] private CanvasGroup freeSpinBgCanvasGroup;
  [SerializeField] private TMP_Text losPolosTextPrefab;
  [SerializeField] private Transform spinCountSnapParent;
  [SerializeField] private RectTransform flyingTextParent;
  [SerializeField] private GameObject freeSpinPopupTitleObject;
  [SerializeField] private TMP_Text freeSpinPopupSpinsText;
  [SerializeField] private GameObject midSumAnimObj;
  [SerializeField] private GameObject dissolveAnimObj;

  [Header("Cash Collect Feature UI")]
  [SerializeField] private CanvasGroup jackpotPanelCanvasGroup;
  [SerializeField] private CanvasGroup coinWinDisplayPanelCanvasGroup;
  [SerializeField] private TMP_Text coinWinDisplayText;
  [SerializeField] private GameObject trailRendererPrefab;
  [Range(0.2f, 3.0f)]
  [SerializeField] private float trailMoveDuration = 1.0f;
  [SerializeField] private float delayBetweenTrails = 0.5f;

  internal int totalFreeSpins = 0;
  private double accumulatedFreeSpinWin = 0f;
  internal double AccumulatedFreeSpinWin => accumulatedFreeSpinWin;

  internal void AddToAccumulatedFreeSpinWin(double amount)
  {
      accumulatedFreeSpinWin += amount;
  }

  private int totalBonusSpins = 3;

  internal bool animationFinish = false;
  internal int multiplierCount = 0;
  private Jackpot currentJackpotData;

  
  [Header("HUD Objects")]
  [SerializeField] private Button slotStartButton;
  [SerializeField] private Button autoSpinButton;
  [SerializeField] private Button autoSpinStopButton;
  internal Button AutoSpinStopButton => autoSpinStopButton;
  [SerializeField] private Button totalBetPlusButton;
  [SerializeField] private Button totalBetMinusButton;

  [SerializeField] private Button turboButton;
  [SerializeField] private GameObject turboOnObject;
  [SerializeField] private CanvasGroup turboFlashCanvasGroup;
  [SerializeField] private Button stopSpinButton;
  [SerializeField] private Button gameExitButton;

  [SerializeField] private TMP_Text balanceText;
  [SerializeField] private TMP_Text totalBetText;
  [SerializeField] private TMP_Text totalWinText;
  [SerializeField] private TMP_Text fsNumText;

  [Header("Settings Panel UI")]
[SerializeField] private GameObject settingsPopupPanelBG;
  [SerializeField] private GameObject settingsPopupPanel;
  [SerializeField] private Button settingsButton;
  [SerializeField] private Button settingsQuitButton;
  [SerializeField] private Slider musicVolumeSlider;
  [SerializeField] private Slider soundVolumeSlider;
  

  private bool isVideoPlaying = false;
  private VideoScenario activeVideoScenario;

  private Tween BalanceTween;
  private Coroutine autoClickCoroutine;

  private void Start()
  {

    if (totalWinText != null) totalWinText.text = "00.00";
    if (featureWinPanel != null) featureWinPanel.SetActive(false);
    if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
    if (featurePopup != null) featurePopup.SetActive(false);
    if (walterStashPopup != null) walterStashPopup.SetActive(false);
    if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);
    if (turboFlashCanvasGroup != null) turboFlashCanvasGroup.gameObject.SetActive(false);

    if (midSumAnimObj != null) midSumAnimObj.SetActive(false);
    if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);

    if (normalBgCanvasGroup != null) normalBgCanvasGroup.alpha = 1f;
    if (freeSpinBgCanvasGroup != null) freeSpinBgCanvasGroup.alpha = 0f;

    
    slotManager.OnBalanceChanged += UpdateBalanceText;
    slotManager.OnTotalBetChanged += UpdateTotalBetText;
    slotManager.OnFreeSpinsChanged += UpdateFreeSpinsText;

    slotManager.OnSpinStateChanged += HandleSpinStateChanged;
    slotManager.OnAutoSpinStateChanged += HandleAutoplayStateChanged;
    slotManager.OnAutoplayCountChanged += UpdateAutoplayCounter;
    slotManager.OnAutoplayStopped += HandleAutoplayStopped;

    if (autoplayStartButton) {
      autoplayStartButton.onClick.RemoveAllListeners();
      autoplayStartButton.onClick.AddListener(() => {
          if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
          OnAutoplayStartPressed();
      });
    }
    if (autoplayPanelClose) {
      autoplayPanelClose.onClick.RemoveAllListeners();
      autoplayPanelClose.onClick.AddListener(() => {
          if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
          CloseAutoplayPanel();
      });
    }
    if (autoplayOptionsDropdown) {
      autoplayOptionsDropdown.ClearOptions();
      List<string> options = new List<string> {
          "Until Feature",
          "100 Spins",
          "50 Spins",
          "30 Spins",
          "15 Spins",
          "10 Spins",
          "5 Spins",
          "3 Spins",
          "" 
      };
      autoplayOptionsDropdown.AddOptions(options);

      DropdownItemDisabler disabler = autoplayOptionsDropdown.gameObject.GetComponent<DropdownItemDisabler>();
      if (disabler == null) {
          disabler = autoplayOptionsDropdown.gameObject.AddComponent<DropdownItemDisabler>();
      }
      disabler.indexesToDisable = new List<int> { options.Count - 1 };
    }
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);
    if (autoplayCounterObject) autoplayCounterObject.SetActive(false);

    if (infoButton != null)
    {
        infoButton.onClick.RemoveAllListeners();
        infoButton.onClick.AddListener(() => {
            if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
            OpenInfoPanel();
        });
    }
    if (infoBackButton != null)
    {
        infoBackButton.onClick.RemoveAllListeners();
        infoBackButton.onClick.AddListener(() => {
            if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
            CloseInfoPanel();
        });
    }
    if (infoPanel != null)
    {
        infoPanel.SetActive(false);
    }
    if (settingsPopupPanelBG != null)
    {
        settingsPopupPanelBG.SetActive(false);
    }
    if (settingsPopupPanel != null)
    {
        settingsPopupPanel.SetActive(false);
    }

    InitializeHUD();

    if (featureSpinButton != null)
    {
        featureSpinButton.onClick.RemoveAllListeners();
        featureSpinButton.onClick.AddListener(() => {
            if (featureSpinButton.IsInteractable() && slotManager.IsBonus && !bonusManager.IsSpinning)
            {
                if (AudioController.Instance != null) AudioController.Instance.PlaySpinBtn();
                bonusManager.StartBonusSlot();
            }
        });
    }
  }

  private void InitializeHUD()
  {
      
      if (slotStartButton) {
          slotStartButton.onClick.RemoveAllListeners();
          
          HoldButtonHandler holdHandler = slotStartButton.gameObject.GetComponent<HoldButtonHandler>();
          if (holdHandler == null) {
              holdHandler = slotStartButton.gameObject.AddComponent<HoldButtonHandler>();
          }
          holdHandler.onClick.RemoveAllListeners();
          holdHandler.onClick.AddListener(() => {
              if (slotStartButton.IsInteractable() && slotManager && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin && !slotManager.IsBonus) {
                  if (AudioController.Instance != null) AudioController.Instance.PlaySpinBtn();
                  slotManager.StartSlots();
                  CanCloseMenu();
              }
          });
          holdHandler.onLongPress.RemoveAllListeners();
          holdHandler.onLongPress.AddListener(() => {
              if (slotStartButton.IsInteractable() && slotManager && !slotManager.IsSpinning && !slotManager.IsAutoSpin && !slotManager.IsBonus) {
                  if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
                  OpenAutoplayPanel();
                  CanCloseMenu();
              }
          });
      }
      if (autoSpinButton) {
          autoSpinButton.onClick.RemoveAllListeners();
          autoSpinButton.onClick.AddListener(() => {
              if (autoSpinButton.IsInteractable()) {
                  if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
                  OpenAutoplayPanel();
                  CanCloseMenu();
              }
          });
      }
      if (autoSpinStopButton) {
          autoSpinStopButton.onClick.RemoveAllListeners();
          autoSpinStopButton.onClick.AddListener(() => { 
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              if (slotManager) slotManager.StopAutoSpin(); 
              CanCloseMenu(); 
          });
      }
      if (totalBetPlusButton) {
          totalBetPlusButton.onClick.RemoveAllListeners();
          totalBetPlusButton.onClick.AddListener(() => { 
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              if (slotManager) slotManager.ChangeBet(true); 
              CanCloseMenu(); 
          });
      }
      if (totalBetMinusButton) {
          totalBetMinusButton.onClick.RemoveAllListeners();
          totalBetMinusButton.onClick.AddListener(() => { 
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              if (slotManager) slotManager.ChangeBet(false); 
              CanCloseMenu(); 
          });
      }

      if (turboButton) {
          turboButton.onClick.RemoveAllListeners();
          turboButton.onClick.AddListener(() => { TurboToggle(); CanCloseMenu(); });
          SetTurboActiveState(slotManager != null ? slotManager.IsTurboOn : false);
      }
      if (stopSpinButton) {
          stopSpinButton.onClick.RemoveAllListeners();
          stopSpinButton.onClick.AddListener(() => { 
              if (AudioController.Instance != null) AudioController.Instance.PlaySpinBtn();
              PerformStop(); 
          });
      }

      if (gameExitButton) {
          gameExitButton.onClick.RemoveAllListeners();
          gameExitButton.onClick.AddListener(() => {
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              if (popupManager != null) {
                  popupManager.ShowExitGamePopup();
              } else {
                  CallOnExitFunction();
              }
              CanCloseMenu();
          });
      }

      if (settingsButton) {
          settingsButton.onClick.RemoveAllListeners();
          settingsButton.onClick.AddListener(() => {
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              OpenSettingsPanel();
              CanCloseMenu();
          });
      }
      if (settingsQuitButton) {
          settingsQuitButton.onClick.RemoveAllListeners();
          settingsQuitButton.onClick.AddListener(() => {
              if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
              CloseSettingsPanel();
              CanCloseMenu();
          });
      }
      if (musicVolumeSlider) {
          if (AudioController.Instance != null) musicVolumeSlider.value = AudioController.Instance.MusicVolume;
          musicVolumeSlider.onValueChanged.RemoveAllListeners();
          musicVolumeSlider.onValueChanged.AddListener((val) => {
              if (AudioController.Instance != null) AudioController.Instance.MusicVolume = val;
          });
      }
      if (soundVolumeSlider) {
          if (AudioController.Instance != null) soundVolumeSlider.value = AudioController.Instance.SfxVolume;
          soundVolumeSlider.onValueChanged.RemoveAllListeners();
          soundVolumeSlider.onValueChanged.AddListener((val) => {
              if (AudioController.Instance != null) AudioController.Instance.SfxVolume = val;
              if (DialoguePopupManager.Instance != null) DialoguePopupManager.Instance.UpdateVolume(val);
          });
      }

      UpdateButtonsState();
  }

  internal void SetNormalSpinButtonActive(bool active)
  {
      if (isVideoPlaying) return;
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
  }

  internal void SetBonusSpinCounter(int count)
  {
      if (spinCounterText != null)
      {
          spinCounterText.text = count.ToString() + "/" + totalBonusSpins.ToString();
      }
      if (autoplayCounterText != null)
      {
          autoplayCounterText.text = count.ToString();
      }
  }

  internal void OpenBonusUI(int totalSpins, double initialWin)
  {
      totalBonusSpins = totalSpins;
      if (featureWinPanel != null) featureWinPanel.SetActive(false);
      if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
      
      
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(false);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
      if (turboButton != null) turboButton.gameObject.SetActive(false);
      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(true);

      if (featureWinText != null) featureWinText.text = FormatSpriteText(FormatStaticValue(initialWin));
      SetBonusSpinCounter(totalSpins);
      UpdateFeatureButtonsState(false, totalSpins);
  }

  internal void CloseBonusUI()
  {
      if (featureWinPanel != null) featureWinPanel.SetActive(false);
      if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);
      if (stopSpinButton != null && slotManager.IsBonus) stopSpinButton.gameObject.SetActive(false);

      
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(true);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(true);
      if (turboButton != null) turboButton.gameObject.SetActive(true);
      
      
      UpdateButtonsState();
  }

  internal void SetFeatureWinText(double value)
  {
      if (featureWinText != null)
          featureWinText.text = FormatSpriteText(FormatStaticValue(value));
  }

  private IEnumerator AutoClickFeatureStart(float delay)
  {
      yield return new WaitForSeconds(delay);
      if (featureStartButton != null && featureStartButton.gameObject.activeSelf && featureStartButton.interactable)
      {
          featureStartButton.onClick.Invoke();
      }
  }

  internal void OpenFeaturePopup(Action onStartClicked)
  {
      if (featurePopup == null || featureStartButton == null)
      {
          onStartClicked?.Invoke();
          return;
      }

      
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(false);
      if (turboButton != null) turboButton.gameObject.SetActive(false);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      bool isFSTriggered = (slotManager.OriginalFeatureTriggerResult != null &&
                            slotManager.OriginalFeatureTriggerResult.payload != null &&
                            slotManager.OriginalFeatureTriggerResult.payload.isFreeSpinTriggered) ||
                           (slotManager.ResultData != null &&
                            slotManager.ResultData.payload != null &&
                            slotManager.ResultData.payload.isFreeSpinTriggered);
      if (slotManager != null && !slotManager.IsBonus && (slotManager.IsFreeSpin || isFSTriggered))
      {
          if (featureTitleObject != null) featureTitleObject.SetActive(false);
          if (freeSpinPopupTitleObject != null) freeSpinPopupTitleObject.SetActive(true);
          if (freeSpinPopupSpinsText != null) freeSpinPopupSpinsText.text = totalFreeSpins.ToString();
      }
      else
      {
          if (featureTitleObject != null) featureTitleObject.SetActive(true);
          if (freeSpinPopupTitleObject != null) freeSpinPopupTitleObject.SetActive(false);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(false);
      }
      if (walterStashPopup != null)
      {
          walterStashPopup.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(true);
      }
      featureStartButton.onClick.RemoveAllListeners();
      featureStartButton.onClick.AddListener(() => {
          if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
          if (autoClickCoroutine != null)
          {
              StopCoroutine(autoClickCoroutine);
              autoClickCoroutine = null;
          }
          featurePopup.SetActive(false);
          onStartClicked?.Invoke();
      });

      if (slotManager != null && (slotManager.IsAutoSpin || slotManager.WasAutoSpinOn))
      {
          if (autoClickCoroutine != null) StopCoroutine(autoClickCoroutine);
          autoClickCoroutine = StartCoroutine(AutoClickFeatureStart(2f));
      }
  }

  internal void OpenFeatureWinPopup(double winAmount, Action onCloseClicked)
  {
      if (featurePopup == null || featureStartButton == null)
      {
          onCloseClicked?.Invoke();
          return;
      }

      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      if (featureTitleObject != null)
      {
          featureTitleObject.SetActive(false);
      }
      if (freeSpinPopupTitleObject != null)
      {
          freeSpinPopupTitleObject.SetActive(false);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(true);
      }
      if (walterStashPopup != null)
      {
          walterStashPopup.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(true);
      }
      if (featureWinAmountText != null)
      {
          featureWinAmountText.text = FormatStaticValue(winAmount);
      }
      featureStartButton.onClick.RemoveAllListeners();
      featureStartButton.onClick.AddListener(() => {
          if (AudioController.Instance != null) AudioController.Instance.PlayNormalBtn();
          if (autoClickCoroutine != null)
          {
              StopCoroutine(autoClickCoroutine);
              autoClickCoroutine = null;
          }
          featurePopup.SetActive(false);
          onCloseClicked?.Invoke();
      });

      if (slotManager != null && (slotManager.IsAutoSpin || slotManager.WasAutoSpinOn))
      {
          if (autoClickCoroutine != null) StopCoroutine(autoClickCoroutine);
          autoClickCoroutine = StartCoroutine(AutoClickFeatureStart(2f));
      }
  }

  internal void OpenWalterStashPopup(double amount, Action onComplete)
  {
      if (featurePopup == null || walterStashPopup == null)
      {
          onComplete?.Invoke();
          return;
      }

      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      if (featureTitleObject != null)
      {
          featureTitleObject.SetActive(false);
      }
      if (freeSpinPopupTitleObject != null)
      {
          freeSpinPopupTitleObject.SetActive(false);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(false);
      }

      if (walterStashAmountText != null)
      {
          walterStashAmountText.gameObject.SetActive(false);
          walterStashAmountText.text = FormatStaticValue(0);
          DOTween.Kill(walterStashAmountText);
      }

      bool isCompleted = false;
      Action safeOnComplete = () =>
      {
          if (isCompleted) return;
          isCompleted = true;
          onComplete?.Invoke();
      };

      bool textAnimStarted = false;

      ImageAnimation imgAnim = walterStashPopup.GetComponentInChildren<ImageAnimation>(true);
      if (imgAnim != null)
      {
          imgAnim.onFrameChanged = (frameIndex) =>
          {
              if (frameIndex == 34)
              {
                  textAnimStarted = true;
                  if (walterStashAmountText != null)
                  {
                      walterStashAmountText.gameObject.SetActive(true);
                      double startVal = 0;
                      string animFormat = GetAnimationFormat(amount);
                      DOTween.To(() => startVal, (val) =>
                      {
                          if (walterStashAmountText != null)
                          {
                              walterStashAmountText.text = amount <= 0 ? "00.00" : val.ToString(animFormat);
                          }
                      }, amount, 0.75f).SetEase(Ease.Linear).SetTarget(walterStashAmountText).OnComplete(() =>
                      {
                          if (walterStashAmountText != null)
                          {
                              walterStashAmountText.text = FormatStaticValue(amount);
                          }
                          StartCoroutine(CloseWalterStashAfterDelay(2f, safeOnComplete));
                      });
                  }
                  else
                  {
                      StartCoroutine(CloseWalterStashAfterDelay(2f, safeOnComplete));
                  }
              }
          };
      }

      walterStashPopup.SetActive(true);

      // Fallback: in case image animation does not exist or doesn't trigger frame 34
      StartCoroutine(WalterStashFallbackTimeout(4f, () => textAnimStarted, safeOnComplete));
  }

  private IEnumerator CloseWalterStashAfterDelay(float delay, Action onComplete)
  {
      yield return new WaitForSeconds(delay);
      if (walterStashPopup != null)
      {
          ImageAnimation imgAnim = walterStashPopup.GetComponentInChildren<ImageAnimation>(true);
          if (imgAnim != null)
          {
              imgAnim.onFrameChanged = null;
          }
          if (walterStashAmountText != null)
          {
              DOTween.Kill(walterStashAmountText);
          }
          walterStashPopup.SetActive(false);
      }
      if (featurePopup != null)
      {
          featurePopup.SetActive(false);
      }
      onComplete?.Invoke();
  }

  private IEnumerator WalterStashFallbackTimeout(float timeout, Func<bool> checkStarted, Action onComplete)
  {
      yield return new WaitForSeconds(timeout);
      if (!checkStarted())
      {
          Debug.LogWarning("[UIManager] Walter Stash text animation did not start in time. Triggering fallback close.");
          StartCoroutine(CloseWalterStashAfterDelay(0f, onComplete));
      }
  }

  internal void UpdateFeatureButtonsState(bool isSpinning, int remaining)
  {
      if (isVideoPlaying) return;
      if (featureSpinButton != null)
      {
          featureSpinButton.gameObject.SetActive(!isSpinning);
          featureSpinButton.interactable = !isSpinning && (remaining > 0);
      }
      if (stopSpinButton != null)
      {
          stopSpinButton.gameObject.SetActive(isSpinning);
          stopSpinButton.interactable = isSpinning;
      }
  }

  internal void AddFreeSpinsText(int count)
  {
      if (fsNumText != null && int.TryParse(fsNumText.text, out int currentVal))
      {
          fsNumText.text = (currentVal + count).ToString();
      }
  }

  internal void SetTotalWinText(string text)
  {
      if (totalWinText) totalWinText.text = text;
  }

  internal void ShowStopButton(bool show)
  {
      if (isVideoPlaying) return;
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(show);
  }

  
  internal void ShowSpinButtonCooldown(bool cooldown)
  {
      if (isVideoPlaying) return;
      if (cooldown)
      {
          if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
          if (slotStartButton)
          {
              slotStartButton.gameObject.SetActive(true);
              slotStartButton.interactable = false;
          }
      }
      else
      {
          UpdateButtonsState();
      }
  }

  private void UpdateBalanceText(double val)
  {
      if (balanceText) balanceText.text = FormatStaticValue(val);
  }

  private void UpdateTotalBetText(double val)
  {
      if (totalBetText) totalBetText.text = val.ToString();
      UpdateJackpotDisplay();
      UpdateInfoMultiplierTexts();
  }

  private void UpdateFreeSpinsText(int val)
  {
      if (fsNumText) fsNumText.text = val.ToString();

      if (spinCounterText != null)
      {
          
          if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.isFreeSpinTriggered)
          {
              return;
          }

          
          if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.totalFreeSpins > 0)
          {
              totalFreeSpins = slotManager.ResultData.payload.totalFreeSpins;
          }

          int spinsUsed = totalFreeSpins - val;
          spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
      }
  }

  internal void SetFreeSpinsActive(bool active)
  {
      if (isVideoPlaying) return;
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
      if (slotStartButton) slotStartButton.interactable = !active;
      if (autoSpinButton) autoSpinButton.gameObject.SetActive(!active);
      if (autoSpinButton) autoSpinButton.interactable = true;
      if (totalBetPlusButton) totalBetPlusButton.interactable = false;
      if (totalBetMinusButton) totalBetMinusButton.interactable = false;
  }

  internal void SetButtonsInteractable(bool toggle)
  {
      if (isVideoPlaying) return;
      if (toggle && slotManager != null && (slotManager.IsBonus || slotManager.IsFeatureTransitioning || slotManager.IsFreeSpin))
      {
          toggle = false;
      }
      if (slotStartButton && !slotManager.IsAutoSpin) slotStartButton.interactable = toggle;
      if (autoSpinButton && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin) autoSpinButton.gameObject.SetActive(toggle);
      if (autoSpinButton && !slotManager.IsAutoSpin) autoSpinButton.interactable = toggle;
      if (totalBetPlusButton) totalBetPlusButton.interactable = (slotManager != null && slotManager.IsAutoSpin) ? false : toggle;
      if (totalBetMinusButton) totalBetMinusButton.interactable = (slotManager != null && slotManager.IsAutoSpin) ? false : toggle;
      
      bool isInfoExitInteractable = slotManager == null || (!slotManager.IsBonus && !slotManager.IsFeatureTransitioning);
      if (infoButton) infoButton.interactable = isInfoExitInteractable;
      if (gameExitButton) gameExitButton.interactable = isInfoExitInteractable;
  }

  internal void SetAutoSpinActive(bool active)
  {
      if (isVideoPlaying) return;
      if (autoSpinStopButton)
      {
          autoSpinStopButton.gameObject.SetActive(active);
          if (active) autoSpinStopButton.interactable = true;
      }
      if (autoSpinButton) autoSpinButton.gameObject.SetActive(!active);
  }

  internal void SetVideoPlaybackState(bool isPlaying, VideoScenario scenario)
  {
      isVideoPlaying = isPlaying;
      activeVideoScenario = scenario;
      if (isPlaying)
      {
          ApplyVideoPlaybackButtonStates(scenario);
      }
      else
      {
          bool isInBonusOrFreeSpin = (slotManager != null && (slotManager.IsFreeSpin || slotManager.IsBonus));
          if (turboButton != null)
          {
              turboButton.gameObject.SetActive(!isInBonusOrFreeSpin);
          }
          UpdateButtonsState();
      }
  }

  private void ApplyVideoPlaybackButtonStates(VideoScenario scenario)
  {
      if (slotStartButton != null) slotStartButton.interactable = false;
      if (stopSpinButton != null) stopSpinButton.interactable = false;
      if (featureSpinButton != null) featureSpinButton.interactable = false;
      if (autoSpinButton != null) autoSpinButton.interactable = false;
      if (autoSpinStopButton != null) autoSpinStopButton.interactable = false;

      bool isInBonusOrFreeSpin = (slotManager != null && (slotManager.IsFreeSpin || slotManager.IsBonus))
          || scenario == VideoScenario.FreeSpinStart 
          || scenario == VideoScenario.FreeSpinEnd 
          || scenario == VideoScenario.LinkFeatureStart 
          || scenario == VideoScenario.LinkFeatureEnd;

      if (!isInBonusOrFreeSpin)
      {
          if (slotStartButton != null)
          {
              slotStartButton.gameObject.SetActive(true);
              slotStartButton.interactable = false;
          }
          if (stopSpinButton != null)
          {
              stopSpinButton.gameObject.SetActive(false);
          }
          if (featureSpinButton != null)
          {
              featureSpinButton.gameObject.SetActive(false);
          }
          if (autoplayCounterObject != null)
          {
              autoplayCounterObject.SetActive(false);
          }
          if (autoSpinButton != null)
          {
              autoSpinButton.gameObject.SetActive(false);
          }
          if (autoSpinStopButton != null)
          {
              autoSpinStopButton.gameObject.SetActive(false);
          }
      }
      else
      {
          if (stopSpinButton != null)
          {
              stopSpinButton.gameObject.SetActive(true);
              stopSpinButton.interactable = false;
          }
          if (slotStartButton != null)
          {
              slotStartButton.gameObject.SetActive(false);
          }
          if (featureSpinButton != null)
          {
              featureSpinButton.gameObject.SetActive(false);
          }
          if (autoplayCounterObject != null)
          {
              autoplayCounterObject.SetActive(false);
          }
          if (autoSpinButton != null)
          {
              autoSpinButton.gameObject.SetActive(false);
          }
          if (autoSpinStopButton != null)
          {
              autoSpinStopButton.gameObject.SetActive(false);
          }
      }

      if (infoButton != null) infoButton.interactable = false;
      if (gameExitButton != null) gameExitButton.interactable = false;
      if (totalBetPlusButton != null) totalBetPlusButton.interactable = false;
      if (totalBetMinusButton != null) totalBetMinusButton.interactable = false;
      if (turboButton != null) turboButton.gameObject.SetActive(false);
  }

  private void SetTurboActiveState(bool active)
  {
      if (turboOnObject != null)
      {
          turboOnObject.SetActive(active);
      }
      else if (turboButton != null)
      {
          Transform foundChild = null;
          for (int i = 0; i < turboButton.transform.childCount; i++)
          {
              Transform child = turboButton.transform.GetChild(i);
              string lowerName = child.name.ToLower();
              if (lowerName.Contains("on") || lowerName.Contains("active") || lowerName.Contains("turbo"))
              {
                  foundChild = child;
                  break;
              }
          }
          if (foundChild == null && turboButton.transform.childCount > 0)
          {
              foundChild = turboButton.transform.GetChild(0);
          }
          if (foundChild != null)
          {
              foundChild.gameObject.SetActive(active);
          }
      }
  }

  private void TurboToggle()
  {
    if (slotManager.IsTurboOn)
    {
      slotManager.IsTurboOn = false;
      SetTurboActiveState(false);
      if (AudioController.Instance != null) AudioController.Instance.PlayTurboOff();
    }
    else
    {
      slotManager.IsTurboOn = true;
      SetTurboActiveState(true);
      if (AudioController.Instance != null) AudioController.Instance.PlayTurboOn();
    }
    TriggerTurboFlash();
  }

  private void TriggerTurboFlash()
  {
      if (turboFlashCanvasGroup != null)
      {
          turboFlashCanvasGroup.DOKill();
          turboFlashCanvasGroup.gameObject.SetActive(true);
          turboFlashCanvasGroup.alpha = 0.8f;
          
          turboFlashCanvasGroup.DOFade(0f, 0.25f).OnComplete(() => {
              turboFlashCanvasGroup.gameObject.SetActive(false);
          });
      }
  }

  internal void CanCloseMenu()
  {
  }

  internal void LowBalPopup()
  {
    
  }

  internal void PopulateWin(int value)
  {
    
  }

  internal void ADfunction()
  {
    
  }

  internal void InitialiseUIData(PaylineData symbolsText)
  {
      UpdateInfoMultiplierTexts();
  }

  internal IEnumerator TrailRendererAnimation(GameObject TrailRendererGO, int textIndex, int coinvalue, bool IsBonus = false)
  {
    yield break;
  }

  internal IEnumerator PlayCashCollectSequence(CashCollectResult result)
  {
      if (result == null || !result.triggered) yield break;

      
      bool hasCashCoins = false;
      if (result.collectedCoins != null)
      {
          foreach (var coin in result.collectedCoins)
          {
              if (coin.symbolId == 13 || coin.symbolId == 15 || coin.symbolId == 16)
              {
                  hasCashCoins = true;
                  break;
              }
          }
      }

      
      if (hasCashCoins)
      {
          if (jackpotPanelCanvasGroup != null)
          {
              jackpotPanelCanvasGroup.DOKill();
              jackpotPanelCanvasGroup.DOFade(0f, 0.5f);
          }
          
          if (coinWinDisplayPanelCanvasGroup != null)
          {
              coinWinDisplayPanelCanvasGroup.DOKill();
              coinWinDisplayPanelCanvasGroup.gameObject.SetActive(true);
              coinWinDisplayPanelCanvasGroup.alpha = 0f;
              coinWinDisplayPanelCanvasGroup.DOFade(1f, 0.5f);
          }

          if (coinWinDisplayText != null)
          {
              coinWinDisplayText.text = "00.00";
          }

          yield return new WaitForSeconds(0.5f);
      }

      
      List<List<int>> ccPositions = new List<List<int>>();
      if (result.positions != null)
      {
          foreach (var pos in result.positions)
          {
              if (pos.Type == Newtonsoft.Json.Linq.JTokenType.Array)
              {
                  int r = (int)pos[0];
                  int c = (int)pos[1];
                  ccPositions.Add(new List<int> { r, c });
              }
          }
      }

      double accumulatedVal = 0f;

      
      if (result.collectedCoins != null && result.collectedCoins.Count > 0 && ccPositions.Count > 0)
      {
          List<CollectedCoin> sortedCoins = new List<CollectedCoin>(result.collectedCoins);
          sortedCoins.Sort((a, b) =>
          {
              if (a.symbolId == 16 && b.symbolId != 16) return -1;
              if (b.symbolId == 16 && a.symbolId != 16) return 1;
              return 0;
          });

          foreach (var ccPos in ccPositions)
          {
              SlotSymbolView ccView = slotManager.GetSymbolView(ccPos[0], ccPos[1]);

              foreach (var coin in sortedCoins)
              {
                  
                  if (coin.symbolId == 15 || coin.symbolId == 16 || coin.symbolId == 13)
                  {
                      int r = coin.position[0];
                      int c = coin.position[1];
                      SlotSymbolView cashCoinView = slotManager.GetSymbolView(r, c);
                      if (cashCoinView == null)
                      {
                          continue;
                      }

                      
                      float animDuration = slotManager.animationManager != null ? slotManager.animationManager.winSymbolLoopDuration / 2f : 0.75f;
                      cashCoinView.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), animDuration, 1, 0.5f);

                      
                      if (slotManager.animationManager != null)
                      {
                          slotManager.animationManager.PlaySpecialAnimationForCell(ccPos[0], ccPos[1]);
                      }

                      
                      if (trailRendererPrefab != null)
                      {
                          if (AudioController.Instance != null) AudioController.Instance.PlayTrailStart();
                          GameObject trInstance = Instantiate(trailRendererPrefab, flyingTextParent != null ? flyingTextParent : transform);
                          
                          
                          trInstance.transform.localScale = Vector3.one;
                          
                          
                          trInstance.transform.position = cashCoinView.transform.position;
                          
                          
                          Vector3 localPos = trInstance.transform.localPosition;
                          localPos.z = 0f;
                          trInstance.transform.localPosition = localPos;

                          
                          double coinAmt = coin.coinValue * slotManager.TotalBet;
                          TMP_Text trText = trInstance.GetComponentInChildren<TMP_Text>();
                          if (trText != null)
                          {
                              trText.text = FormatStaticValue(coinAmt);
                          }

                          
                          Vector3 targetPos = coinWinDisplayPanelCanvasGroup != null ? coinWinDisplayPanelCanvasGroup.transform.position : Vector3.zero;
                          
                          double finalTarget = accumulatedVal + coinAmt;
                          accumulatedVal = finalTarget;

                          bool trailCompleted = false;
                          trInstance.transform.DOMove(targetPos, trailMoveDuration).SetEase(Ease.OutQuad).OnComplete(() =>
                          {
                              Destroy(trInstance);
                              
                              double currentVal = 0;
                              if (coinWinDisplayText != null)
                              {
                                  double.TryParse(coinWinDisplayText.text, out currentVal);
                              }
                              
                              string coinFormat = GetAnimationFormat(finalTarget);
                              DOTween.To(() => currentVal, (val) =>
                              {
                                  if (coinWinDisplayText != null)
                                      coinWinDisplayText.text = finalTarget <= 0 ? "00.00" : val.ToString(coinFormat);
                              }, finalTarget, 0.3f).OnComplete(() =>
                              {
                                  if (coinWinDisplayText != null)
                                      coinWinDisplayText.text = FormatStaticValue(finalTarget);
                                  trailCompleted = true;
                              });
                          });

                          
                          yield return new WaitUntil(() => trailCompleted);
                      }
                      else
                      {
                          
                          double startVal = accumulatedVal;
                          accumulatedVal += coin.coinValue * slotManager.TotalBet;
                          double endVal = accumulatedVal;
                          if (coinWinDisplayText != null)
                              coinWinDisplayText.text = FormatStaticValue(endVal);
                      }

                      
                      yield return new WaitForSeconds(delayBetweenTrails);
                  }
              }
          }
      }

      yield return new WaitForSeconds(1.0f);

      
      foreach (var ccPos in ccPositions)
      {
          if (slotManager.animationManager != null)
          {
              slotManager.animationManager.StopSymbolAnimationLoop(ccPos[0], ccPos[1]);
          }
      }

      
      if (hasCashCoins)
      {
          if (coinWinDisplayPanelCanvasGroup != null)
          {
              coinWinDisplayPanelCanvasGroup.DOKill();
              coinWinDisplayPanelCanvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
              {
                  coinWinDisplayPanelCanvasGroup.gameObject.SetActive(false);
              });
          }

          if (jackpotPanelCanvasGroup != null)
          {
              jackpotPanelCanvasGroup.DOKill();
              jackpotPanelCanvasGroup.DOFade(1f, 0.5f);
          }

          yield return new WaitForSeconds(0.5f);
      }
  }

  internal IEnumerator MidGameImageAnimation(ImageAnimation imageAnimation, double num = 0)
  {
    yield break;
  }

  private void CallOnExitFunction()
  {
    slotManager.CallCloseSocket();
  }

  internal void SetJackpotText(Jackpot jackpot)
  {
    currentJackpotData = jackpot;
    UpdateJackpotDisplay();
  }

  internal void UpdateJackpotDisplay()
  {
    if (currentJackpotData == null || currentJackpotData.payout == null || JackpotText == null) return;

    double totalBet = slotManager != null ? slotManager.TotalBet : 0;

    for (int i = 0; i < currentJackpotData.payout.Count; i++)
    {
      if (i >= JackpotText.Count) break;
      if (JackpotText[i] != null)
      {
        double multiplier = currentJackpotData.payout[i];
        double jackpotValue = multiplier * totalBet;
        JackpotText[i].text = FormatStaticValue(jackpotValue);
      }
    }
  }

  internal void DisconnectionPopup()
  {
    
  }

  internal void CheckAndClosePopups()
  {
    
  }

  internal void ReconnectionPopup()
  {
    
  }

  internal void OpenFreeSpinsUI()
  {
      if (fsNumText) fsNumText.text = slotManager.FreeSpinsCount.ToString();

      if (spinCounterPanel != null) spinCounterPanel.SetActive(true);
      if (featureWinPanel != null) featureWinPanel.SetActive(true);

      if (spinCounterText != null)
      {
          spinCounterText.gameObject.SetActive(true);
          if (totalFreeSpins <= 0 && slotManager.ResultData != null && slotManager.ResultData.payload != null)
          {
              totalFreeSpins = slotManager.ResultData.payload.totalFreeSpins;
          }
          int spinsUsed = totalFreeSpins - slotManager.FreeSpinsCount;
          spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
      }

      if (featureWinText != null)
      {
          featureWinText.gameObject.SetActive(true);
          featureWinText.text = FormatSpriteText(FormatStaticValue(accumulatedFreeSpinWin));
      }
  }

  internal void CloseFreeSpinsUI()
  {
    if (AudioController.Instance != null) AudioController.Instance.PlayMainBg();
    slotManager.IsFreeSpin = false;
    if (fsNumText) fsNumText.text = "0";
    totalFreeSpins = 0;

    slotManager.IsFeatureTransitioning = true;
    UpdateButtonsState();

    if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
    if (featureWinPanel != null) featureWinPanel.SetActive(false);
    if (spinCounterText != null) spinCounterText.gameObject.SetActive(true);
    if (featureWinText != null) featureWinText.gameObject.SetActive(true);

    
    if (slotStartButton != null) slotStartButton.gameObject.SetActive(true);
    if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(true);
    if (turboButton != null) turboButton.gameObject.SetActive(true);

    float fadeDuration = 1f;
    if (normalBgCanvasGroup != null) normalBgCanvasGroup.DOFade(1f, fadeDuration);
    if (freeSpinBgCanvasGroup != null)
    {
        freeSpinBgCanvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            slotManager.IsFeatureTransitioning = false;
            UpdateButtonsState();
        });
    }
    else
    {
        slotManager.IsFeatureTransitioning = false;
        UpdateButtonsState();
    }
  }

  internal void WinningsTextAnimation(Action onComplete = null, double? customWinAmt = null)
  {
    double winAmt = customWinAmt ?? slotManager.WinAmount;
    double threshold = winTypePopup != null ? winTypePopup.EnableWinThreshold : 3.0;
    if (winAmt >= slotManager.TotalBet * threshold && winTypePopup != null)
    {
      bool isAutoMode = slotManager.IsFreeSpin || slotManager.IsAutoSpin;
      winTypePopup.StartPopup(winAmt, slotManager.TotalBet, isAutoMode, () =>
      {
          if (totalWinText) totalWinText.text = FormatStaticValue(winAmt);

          if (slotManager.IsFreeSpin)
          {
              double targetFeatureWin = 0;
              if (socketManager != null && socketManager.resultData != null && socketManager.resultData.features != null)
              {
                  targetFeatureWin = socketManager.resultData.features.featureWin;
              }
              else
              {
                  if (!customWinAmt.HasValue)
                  {
                      accumulatedFreeSpinWin += winAmt;
                  }
                  targetFeatureWin = accumulatedFreeSpinWin;
              }
              accumulatedFreeSpinWin = targetFeatureWin;
              if (featureWinText) featureWinText.text = FormatSpriteText(FormatStaticValue(targetFeatureWin));
          }

          double Balance = 0;
          if (double.TryParse(FormatStaticValue(socketManager.playerdata.balance), out double bal))
          {
              Balance = bal;
          }
          if (balanceText) balanceText.text = FormatStaticValue(Balance);

          onComplete?.Invoke();
      });
      return;
    }

    if (!double.TryParse(balanceText.text, out double currentBal))
    {
      
    }
    if (!double.TryParse(FormatStaticValue(socketManager.playerdata.balance), out double Balance))
    {
      
    }
    if (!double.TryParse(totalWinText.text, out double currentWin))
    {
      
    }

    int completedTweens = 0;
    int targetTweensCount = slotManager.IsFreeSpin ? 3 : 2;

    Action checkComplete = () =>
    {
        completedTweens++;
        if (completedTweens >= targetTweensCount)
        {
            onComplete?.Invoke();
        }
    };

    string winFormat = GetAnimationFormat(winAmt);
    DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
    {
      if (totalWinText) totalWinText.text = winAmt <= 0 ? "00.00" : currentWin.ToString(winFormat);
    }).OnComplete(() => {
      if (totalWinText) totalWinText.text = FormatStaticValue(winAmt);
      checkComplete();
    });

    if (slotManager.IsFreeSpin)
    {
      double targetFeatureWin = 0;
      if (socketManager != null && socketManager.resultData != null && socketManager.resultData.features != null)
      {
          targetFeatureWin = socketManager.resultData.features.featureWin;
      }
      else
      {
          if (!customWinAmt.HasValue)
          {
              accumulatedFreeSpinWin += winAmt;
          }
          targetFeatureWin = accumulatedFreeSpinWin;
      }
      accumulatedFreeSpinWin = targetFeatureWin;
      double tempWin = targetFeatureWin - winAmt;
      if (tempWin < 0) tempWin = 0;
      string featFormat = GetAnimationFormat(targetFeatureWin);
      DOTween.To(() => tempWin, (val) => tempWin = val, targetFeatureWin, 0.8f).OnUpdate(() =>
      {
         if (featureWinText) featureWinText.text = FormatSpriteText(targetFeatureWin <= 0 ? "00.00" : tempWin.ToString(featFormat));
      }).OnComplete(() => {
         if (featureWinText) featureWinText.text = FormatSpriteText(FormatStaticValue(targetFeatureWin));
         checkComplete();
      });
    }

    BalanceTween?.Kill();
    string balFormat = GetAnimationFormat(Balance);
    BalanceTween = DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
    {
      if (balanceText) balanceText.text = Balance <= 0 ? "00.00" : currentBal.ToString(balFormat);
    }).OnComplete(() => {
      if (balanceText) balanceText.text = FormatStaticValue(Balance);
      checkComplete();
    });
  }

  internal void DeductBalanceUI()
  {
    BalanceTween?.Kill();
    double bet = slotManager.TotalBet;
    double balance = slotManager.Balance;
    balance -= bet;
    if (balanceText) balanceText.text = FormatStaticValue(balance);
  }
  
  internal void SwitchTopUI(bool trigger)
  {
    
  }

  

  private void OpenAutoplayPanel()
  {
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(true);
  }

  private void CloseAutoplayPanel()
  {
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);
  }

  private void OpenInfoPanel()
  {
      if (infoPanel != null)
      {
          infoPanel.SetActive(true);
          UpdateInfoMultiplierTexts();
      }
  }

  private void CloseInfoPanel()
  {
      if (infoPanel != null)
      {
          infoPanel.SetActive(false);
      }
  }

  private void OpenSettingsPanel()
  {
      if (settingsPopupPanelBG != null)
      {
          settingsPopupPanelBG.SetActive(true);
      }
      if (settingsPopupPanel != null)
      {
          settingsPopupPanel.SetActive(true);
          settingsPopupPanel.transform.localScale = Vector3.zero;
          settingsPopupPanel.transform.DOKill();
          settingsPopupPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
      }
  }

  private void CloseSettingsPanel()
  {
      if (settingsPopupPanel != null)
      {
          settingsPopupPanel.transform.DOKill();
          Sequence closeSeq = DOTween.Sequence();
          closeSeq.Append(settingsPopupPanel.transform.DOScale(1.1f, 0.1f));
          closeSeq.Append(settingsPopupPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
          closeSeq.OnComplete(() =>
          {
              settingsPopupPanel.SetActive(false);
              settingsPopupPanel.transform.localScale = Vector3.one;
              if (settingsPopupPanelBG != null)
              {
                  settingsPopupPanelBG.SetActive(false);
              }
          });
          closeSeq.SetUpdate(true);
      }
      else
      {
          if (settingsPopupPanelBG != null)
          {
              settingsPopupPanelBG.SetActive(false);
          }
      }
  }

  internal void CloseAllPanels()
  {
      CloseAutoplayPanel();
      CloseInfoPanel();
      if (settingsPopupPanel != null) settingsPopupPanel.SetActive(false);
      if (settingsPopupPanelBG != null) settingsPopupPanelBG.SetActive(false);
  }

  private void UpdateInfoMultiplierTexts()
  {
      if (slotManager == null || symbolTextMaps == null) return;

      double lineBet = slotManager.LineBet;

      foreach (var map in symbolTextMaps)
      {
          if (map.textComponent == null) continue;

          var symbolInfo = slotManager.GetSymbolInfo(map.symbolId);
          if (symbolInfo != null && symbolInfo.multiplier != null && symbolInfo.multiplier.Count >= 3)
          {
              double mult5 = symbolInfo.multiplier[0];
              double mult4 = symbolInfo.multiplier[1];
              double mult3 = symbolInfo.multiplier[2];

              double val5 = lineBet * mult5;
              double val4 = lineBet * mult4;
              double val3 = lineBet * mult3;

              map.textComponent.text = $"5x {FormatStaticValue(val5)}\n4x {FormatStaticValue(val4)}\n3x {FormatStaticValue(val3)}";
          }
      }
  }

  private void OnAutoplayStartPressed()
  {
    if (!autoplayOptionsDropdown) return;

    int selectedIndex = autoplayOptionsDropdown.value;
    DropdownItemDisabler disabler = autoplayOptionsDropdown.GetComponent<DropdownItemDisabler>();
    if (disabler != null && disabler.SelectedIndexOverride != -1)
    {
      selectedIndex = disabler.SelectedIndexOverride;
    }
    string selectedText = autoplayOptionsDropdown.options[selectedIndex].text;

    int spinCount = 0;
    bool untilFeature = false;

    if (selectedText.Equals("Until Feature", StringComparison.OrdinalIgnoreCase))
    {
      untilFeature = true;
      spinCount = -1;
    }
    else
    {
      string[] parts = selectedText.Split(' ');
      if (parts.Length > 0 && int.TryParse(parts[0], out int count))
      {
        spinCount = count;
      }
      else
      {
        spinCount = 100; 
      }
    }

    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);

    
    slotManager.StartAutoplay(spinCount, untilFeature);
  }

  private void HandleSpinStateChanged(bool isSpinning)
  {
    UpdateButtonsState();
  }

  private void HandleAutoplayStateChanged(bool isAutoSpin)
  {
    UpdateButtonsState();
  }

  private void UpdateAutoplayCounter(int count, bool untilFeature)
  {
    if (untilFeature)
    {
      if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    }
    else
    {
      if (autoplayCounterObject) autoplayCounterObject.SetActive(true);
      if (autoplayCounterText) autoplayCounterText.text = count.ToString();
    }
  }

  private void HandleAutoplayStopped()
  {
    if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    UpdateButtonsState();
  }

  internal void UpdateButtonsState()
  {
    if (slotManager == null) return;

    if (isVideoPlaying)
    {
        ApplyVideoPlaybackButtonStates(activeVideoScenario);
        return;
    }

    if (DialoguePopupManager.Instance != null && DialoguePopupManager.Instance.IsDialogueActive)
    {
        if (slotStartButton) slotStartButton.interactable = false;
        if (stopSpinButton) stopSpinButton.interactable = false;
        if (featureSpinButton) featureSpinButton.interactable = false;
        if (autoSpinButton) autoSpinButton.interactable = false;
        if (autoSpinStopButton) autoSpinStopButton.interactable = false;
        return;
    }

    
    if ((featurePopup != null && featurePopup.activeSelf) || (walterStashPopup != null && walterStashPopup.activeSelf))
    {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (turboButton) turboButton.gameObject.SetActive(false);
        if (autoSpinButton) autoSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
        if (featureSpinButton) featureSpinButton.gameObject.SetActive(false);
        if (infoButton) infoButton.interactable = false;
        if (gameExitButton) gameExitButton.interactable = false;
        return;
    }

    if (slotManager.IsBonus)
    {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (autoSpinButton) autoSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(true);

        bool isBonusSpinning = bonusManager != null && bonusManager.IsSpinning;
        int remaining = slotManager.LinkRespinsRemaining;
        if (featureSpinButton)
        {
            featureSpinButton.gameObject.SetActive(!isBonusSpinning);
            featureSpinButton.interactable = !isBonusSpinning && (remaining > 0);
        }
        if (stopSpinButton)
        {
            stopSpinButton.gameObject.SetActive(isBonusSpinning);
            stopSpinButton.interactable = isBonusSpinning;
        }

        if (infoButton) infoButton.interactable = false;
        if (gameExitButton) gameExitButton.interactable = false;
        if (totalBetPlusButton) totalBetPlusButton.interactable = false;
        if (totalBetMinusButton) totalBetMinusButton.interactable = false;
        if (turboButton) turboButton.gameObject.SetActive(false);
        return;
    }

    
    bool isInfoExitInteractable = !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
    if (infoButton) infoButton.interactable = isInfoExitInteractable;
    if (gameExitButton) gameExitButton.interactable = isInfoExitInteractable;

    
    bool isBetInteractable = !slotManager.IsSpinning && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin && !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
    if (totalBetPlusButton) totalBetPlusButton.interactable = isBetInteractable;
    if (totalBetMinusButton) totalBetMinusButton.interactable = isBetInteractable;

    if (slotManager.IsSpinning)
    {
      if (slotManager.IsAutoSpin)
      {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton)
        {
          autoSpinStopButton.gameObject.SetActive(true);
          autoSpinStopButton.interactable = true;
        }
        if (autoplayCounterObject) autoplayCounterObject.SetActive(!slotManager.AutoplayUntilFeature);
      }
      else
      {
        if (slotManager.IsAutoplayStoppedMidSpin)
        {
          if (slotStartButton)
          {
            slotStartButton.gameObject.SetActive(true);
            slotStartButton.interactable = false;
          }
          if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
          if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
          if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
        }
        else
        {
          if (slotStartButton) slotStartButton.gameObject.SetActive(false);
          if (stopSpinButton)
          {
            stopSpinButton.gameObject.SetActive(true);
            stopSpinButton.interactable = true;
          }
          if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
          if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);
        }
      }
    }
    else
    {
      if (slotManager.IsAutoSpin)
      {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton)
        {
          autoSpinStopButton.gameObject.SetActive(true);
          autoSpinStopButton.interactable = true;
        }
        if (autoplayCounterObject) autoplayCounterObject.SetActive(!slotManager.AutoplayUntilFeature);
      }
      else if (slotManager.IsFreeSpin)
      {
        if (slotStartButton) {
          slotStartButton.gameObject.SetActive(true);
          slotStartButton.interactable = false;
        }
        if (stopSpinButton)
        {
          stopSpinButton.gameObject.SetActive(true);
          stopSpinButton.interactable = false;
          if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        }
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
      }
      else
      {
        if (slotStartButton) {
          slotStartButton.gameObject.SetActive(true);
          
          slotStartButton.interactable = !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
        }
        if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);

        
        if (slotManager.IsBonus || slotManager.IsFeatureTransitioning)
        {
            SetButtonsInteractable(false);
        }
      }
    }

    if (slotManager.IsFeatureTransitioning && !slotManager.IsSpinning)
    {
        if (slotStartButton) slotStartButton.interactable = false;
        if (autoSpinButton) autoSpinButton.interactable = false;
        if (stopSpinButton) stopSpinButton.interactable = false;
        if (featureSpinButton) featureSpinButton.interactable = false;
        if (turboButton) turboButton.interactable = false;
        SetButtonsInteractable(false);
    }
  }

  internal void PerformStop()
  {
      if (slotManager == null) return;

      if (slotManager.IsBonus)
      {
          if (bonusManager != null && bonusManager.IsSpinning)
          {
              bonusManager.StopSpinToggle = true;
              
              UpdateFeatureButtonsState(false, slotManager.LinkRespinsRemaining);
              if (featureSpinButton != null) featureSpinButton.interactable = false;
          }
      }
      else
      {
          if (slotManager.IsSpinning && !slotManager.StopSpinToggle)
          {
              slotManager.StopSpinToggle = true;
              
              ShowSpinButtonCooldown(true);
          }
      }
  }

  internal static int GetDecimalPlaces(double value)
  {
      double rounded = Math.Round(value, 3);
      string s = rounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
      int dotIndex = s.IndexOf('.');
      if (dotIndex < 0) return 0;
      return s.Length - dotIndex - 1;
  }

  internal static string FormatStaticValue(double value)
  {
      if (value <= 0)
      {
          return "00.00";
      }
      return value.ToString("0.###");
  }

  internal static string GetAnimationFormat(double resultAmount)
  {
      int decimals = GetDecimalPlaces(resultAmount);
      if (decimals <= 1)
      {
          return "0.0";
      }
      else if (decimals == 2)
      {
          return "0.00";
      }
      else
      {
          return "0.000";
      }
  }

  private string FormatSpriteText(string input)
  {
      string result = "";
      foreach (char c in input)
      {
          if (char.IsDigit(c))
          {
              result += $"<sprite={c - '0'}>";
          }
          else if (c == '.')
          {
              result += "<sprite=10>";
          }
          else if (c == ',')
          {
              result += "<sprite=11>";
          }
          else
          {
              result += c;
          }
      }
      return result;
  }

  internal IEnumerator PlayFreeSpinTriggerSequence(FreeSpinResult fsResult, bool isRetrigger = false, bool fromBonusSlot = false)
  {
      if (slotManager != null)
      {
          slotManager.IsFeatureTransitioning = true;
      }
      UpdateButtonsState();

      if (midSumAnimObj != null) midSumAnimObj.SetActive(false);
      if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);

      bool isConflict = fromBonusSlot && slotManager != null && slotManager.OriginalFeatureTriggerResult != null && slotManager.OriginalFeatureTriggerResult.payload != null && slotManager.OriginalFeatureTriggerResult.payload.isLinkTriggered && slotManager.OriginalFeatureTriggerResult.payload.isFreeSpinTriggered;
      int originalFreeSpins = 0;
      if (isConflict)
      {
          var originalFsResult = slotManager.OriginalFeatureTriggerResult.payload.freeSpinResult;
          if (originalFsResult != null && originalFsResult.triggerCoins != null && originalFsResult.triggerCoins.Count > 0)
          {
              foreach (var coin in originalFsResult.triggerCoins)
              {
                  originalFreeSpins += (int)coin.coinValue;
              }
          }
          else
          {
              originalFreeSpins = originalFsResult != null ? originalFsResult.freeSpinCount : 0;
          }
      }

      int totalSpins = 0;
      if (fsResult != null && fsResult.triggerCoins != null && fsResult.triggerCoins.Count > 0)
      {
          foreach (var coin in fsResult.triggerCoins)
          {
              totalSpins += (int)coin.coinValue;
          }
      }
      else
      {
          totalSpins = fsResult != null ? fsResult.freeSpinCount : (slotManager != null ? slotManager.FreeSpinsCount : 0);
      }

      int previousRemaining = 0;
      int previousTotal = 0;

      if (fsResult != null)
      {
          previousRemaining = fsResult.freeSpinsRemaining - totalSpins;
          if (fsResult.totalFreeSpins > 0)
          {
              previousTotal = fsResult.totalFreeSpins - totalSpins;
          }
          else
          {
              previousTotal = totalFreeSpins; 
          }
      }
      else
      {
          previousRemaining = slotManager != null ? slotManager.FreeSpinsCount : 0;
          previousTotal = totalFreeSpins;
      }

      int previousSpinsUsed = previousTotal - previousRemaining;
      if (previousSpinsUsed < 0) previousSpinsUsed = 0;

      if (isRetrigger)
      {
          if (spinCounterPanel != null) spinCounterPanel.SetActive(true);
          if (featureWinPanel != null) featureWinPanel.SetActive(true);
          if (spinCounterText != null)
          {
              spinCounterText.text = $"{previousSpinsUsed}/{previousTotal}";
              spinCounterText.gameObject.SetActive(true);
          }
          if (featureWinText != null)
          {
              featureWinText.text = FormatSpriteText(FormatStaticValue(accumulatedFreeSpinWin));
              featureWinText.gameObject.SetActive(true);
          }
      }
      else
      {
          if (fromBonusSlot && socketManager != null && socketManager.resultData != null && socketManager.resultData.features != null)
          {
              accumulatedFreeSpinWin = socketManager.resultData.features.featureWin;
          }
          else
          {
              accumulatedFreeSpinWin = 0f;
          }

          if (AudioController.Instance != null) AudioController.Instance.PlayBonusBg();
          if (normalBgCanvasGroup != null) normalBgCanvasGroup.DOFade(0f, 1f);
          if (freeSpinBgCanvasGroup != null) freeSpinBgCanvasGroup.DOFade(1f, 1f);

          if (spinCounterPanel != null) spinCounterPanel.SetActive(true);
          if (featureWinPanel != null) featureWinPanel.SetActive(true);

          if (spinCounterText != null) spinCounterText.gameObject.SetActive(false);
          if (featureWinText != null) featureWinText.gameObject.SetActive(false);
      }

      yield return new WaitForSeconds(0.5f);

      List<Vector3> coinWorldPositions = new List<Vector3>();
      List<string> coinTexts = new List<string>();
      List<TMP_Text> sourceTexts = new List<TMP_Text>();

      if (fsResult != null && fsResult.triggerCoins != null && fsResult.triggerCoins.Count > 0)
      {
          foreach (var coin in fsResult.triggerCoins)
          {
              int row = coin.position[0];
              int col = coin.position[1];

              if (fromBonusSlot)
              {
                  if (bonusManager != null && bonusManager.Slot != null && col < bonusManager.Slot.Count)
                  {
                      var rowTransforms = bonusManager.Slot[col].slotTransforms;
                      if (row < rowTransforms.Count)
                      {
                          var view = rowTransforms[row].GetChild(2).GetComponent<SlotSymbolView>();
                          if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                          {
                              coinWorldPositions.Add(view.losPolosValueText.transform.position);
                              coinTexts.Add(view.losPolosValueText.text);
                              sourceTexts.Add(view.losPolosValueText);
                          }
                      }
                  }
              }
              else
              {
                  if (slotManager != null && slotManager.ResultMatrix != null && row < slotManager.ResultMatrix.Count)
                  {
                      var rowImages = slotManager.ResultMatrix[row].slotImages;
                      if (col < rowImages.Count)
                      {
                          var view = rowImages[col].GetComponent<SlotSymbolView>();
                          if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                          {
                              coinWorldPositions.Add(view.losPolosValueText.transform.position);
                              coinTexts.Add(view.losPolosValueText.text);
                              sourceTexts.Add(view.losPolosValueText);
                          }
                      }
                  }
              }
          }
      }
      else
      {
          if (fromBonusSlot)
          {
              if (bonusManager != null && bonusManager.Slot != null)
              {
                  for (int col = 0; col < bonusManager.Slot.Count; col++)
                  {
                      var rowTransforms = bonusManager.Slot[col].slotTransforms;
                      for (int row = 0; row < rowTransforms.Count; row++)
                      {
                          var triggerResult = (slotManager != null && slotManager.OriginalFeatureTriggerResult != null)
                                ? slotManager.OriginalFeatureTriggerResult
                                : (socketManager != null ? socketManager.resultData : null);

                          if (triggerResult != null && triggerResult.matrix != null)
                          {
                              if (row < triggerResult.matrix.Count && col < triggerResult.matrix[row].Count)
                              {
                                  if (triggerResult.matrix[row][col] == "17")
                                  {
                                      var view = rowTransforms[row].GetChild(2).GetComponent<SlotSymbolView>();
                                      if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                                      {
                                          coinWorldPositions.Add(view.losPolosValueText.transform.position);
                                          coinTexts.Add(view.losPolosValueText.text);
                                          sourceTexts.Add(view.losPolosValueText);
                                      }
                                  }
                              }
                          }
                      }
                  }
              }
          }
          else
          {
              if (slotManager != null && slotManager.ResultMatrix != null)
              {
                  for (int r = 0; r < slotManager.ResultMatrix.Count; r++)
                  {
                      var rowImages = slotManager.ResultMatrix[r].slotImages;
                      for (int c = 0; c < rowImages.Count; c++)
                      {
                           var triggerResult = (slotManager != null && slotManager.OriginalFeatureTriggerResult != null)
                               ? slotManager.OriginalFeatureTriggerResult
                               : (socketManager != null ? socketManager.resultData : null);

                           if (triggerResult != null && triggerResult.matrix != null)
                           {
                               if (r < triggerResult.matrix.Count && c < triggerResult.matrix[r].Count)
                               {
                                   if (triggerResult.matrix[r][c] == "17")
                                   {
                                       var view = rowImages[c].GetComponent<SlotSymbolView>();
                                       if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                                       {
                                           coinWorldPositions.Add(view.losPolosValueText.transform.position);
                                           coinTexts.Add(view.losPolosValueText.text);
                                           sourceTexts.Add(view.losPolosValueText);
                                       }
                                   }
                               }
                           }
                      }
                  }
              }
          }
      }

      if (fsResult != null && fsResult.totalFreeSpins > 0)
      {
          totalFreeSpins = fsResult.totalFreeSpins;
      }
      else if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.totalFreeSpins > 0)
      {
          totalFreeSpins = slotManager.ResultData.payload.totalFreeSpins;
      }
      else
      {
          if (isRetrigger)
          {
              totalFreeSpins += totalSpins;
          }
          else
          {
              totalFreeSpins = totalSpins;
          }
      }

      Transform spawnParent = flyingTextParent != null ? flyingTextParent : this.transform;
      List<TMP_Text> tempTexts = new List<TMP_Text>();
      List<Vector3> initialLocalScales = new List<Vector3>();

      for (int i = 0; i < coinWorldPositions.Count; i++)
      {
          TMP_Text tempText = Instantiate(losPolosTextPrefab, spawnParent);
          tempText.transform.position = coinWorldPositions[i];
          tempText.text = coinTexts[i];

          if (i < sourceTexts.Count && sourceTexts[i] != null)
          {
              Vector3 sourceLossyScale = sourceTexts[i].transform.lossyScale;
              Vector3 parentLossyScale = spawnParent.lossyScale;
              Vector3 matchedLocalScale = new Vector3(
                  parentLossyScale.x != 0 ? sourceLossyScale.x / parentLossyScale.x : 1f,
                  parentLossyScale.y != 0 ? sourceLossyScale.y / parentLossyScale.y : 1f,
                  parentLossyScale.z != 0 ? sourceLossyScale.z / parentLossyScale.z : 1f
              );
              tempText.transform.localScale = matchedLocalScale;
              initialLocalScales.Add(matchedLocalScale);

              RectTransform rectTrans = tempText.GetComponent<RectTransform>();
              RectTransform sourceRectTrans = sourceTexts[i].GetComponent<RectTransform>();
              if (rectTrans != null && sourceRectTrans != null)
              {
                  rectTrans.sizeDelta = sourceRectTrans.sizeDelta;
              }

              sourceTexts[i].gameObject.SetActive(false);
          }
          else
          {
              initialLocalScales.Add(Vector3.one);
          }

          tempText.gameObject.SetActive(true);
          tempTexts.Add(tempText);
      }

      yield return new WaitForSeconds(0.2f);

      Vector3 centerWorldPos = spawnParent.position;
      float moveDuration = 0.8f;
      foreach (var txt in tempTexts)
      {
          txt.transform.DOMove(centerWorldPos, moveDuration).SetEase(Ease.OutQuad);
      }

      yield return new WaitForSeconds(moveDuration);

      if (midSumAnimObj != null) midSumAnimObj.SetActive(true);
      if (AudioController.Instance != null) AudioController.Instance.PlayFlyingTextSpark();

      TMP_Text sumTextPrefab = null;
      Vector3 sumInitialScale = Vector3.one;
      if (tempTexts.Count > 0)
      {
          sumTextPrefab = tempTexts[0];
          sumInitialScale = initialLocalScales[0];
          string sumSpriteText = "<sprite=10>";
          string totalSpinsStr = totalSpins.ToString();
          foreach (char c in totalSpinsStr)
          {
              if (char.IsDigit(c))
              {
                  sumSpriteText += $"<sprite={c - '0'}>";
              }
          }
          sumTextPrefab.text = sumSpriteText;

          for (int i = 1; i < tempTexts.Count; i++)
          {
              if (tempTexts[i] != null) Destroy(tempTexts[i].gameObject);
          }
          tempTexts.Clear();
      }

      if (sumTextPrefab != null)
      {
          sumTextPrefab.transform.DOScale(sumInitialScale * 1.8f, 0.4f).SetEase(Ease.OutBack);
          yield return new WaitForSeconds(0.4f);
          sumTextPrefab.transform.DOScale(sumInitialScale * 1.4f, 0.3f).SetEase(Ease.InQuad);
          yield return new WaitForSeconds(0.3f);

          yield return new WaitForSeconds(0.5f);

          Vector3 targetPos = spinCounterPanel != null ? spinCounterPanel.transform.position : centerWorldPos;
          if (spinCountSnapParent != null)
          {
              targetPos = spinCountSnapParent.position;
          }

          if (midSumAnimObj != null) midSumAnimObj.SetActive(false);

          sumTextPrefab.transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutQuad);
          yield return new WaitForSeconds(moveDuration);

          if (dissolveAnimObj != null) dissolveAnimObj.SetActive(true);
          if (AudioController.Instance != null) AudioController.Instance.PlayFlyingTextSpark();

          if (spinCounterText != null)
          {
              int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
              if (isConflict)
              {
                  remaining -= originalFreeSpins;
              }
              int spinsUsed = (totalFreeSpins - (isConflict ? originalFreeSpins : 0)) - remaining;
              spinCounterText.text = $"{spinsUsed}/{totalFreeSpins - (isConflict ? originalFreeSpins : 0)}";
              spinCounterText.gameObject.SetActive(true);
          }
          Destroy(sumTextPrefab.gameObject);

          yield return new WaitForSeconds(1.0f);
          if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);
      }
      else
      {
          if (spinCounterText != null)
          {
              int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
              if (isConflict)
              {
                  remaining -= originalFreeSpins;
              }
              int spinsUsed = (totalFreeSpins - (isConflict ? originalFreeSpins : 0)) - remaining;
              spinCounterText.text = $"{spinsUsed}/{totalFreeSpins - (isConflict ? originalFreeSpins : 0)}";
              spinCounterText.gameObject.SetActive(true);
          }
      }

      if (fromBonusSlot)
      {
          yield return bonusManager.TransitionFromBonusToNormalSlot();
      }

      if (isConflict)
      {
          List<Vector3> originalCoinWorldPositions = new List<Vector3>();
          List<string> originalCoinTexts = new List<string>();
          List<TMP_Text> originalSourceTexts = new List<TMP_Text>();

          var originalFsResult = slotManager.OriginalFeatureTriggerResult.payload.freeSpinResult;
          if (originalFsResult != null && originalFsResult.triggerCoins != null && originalFsResult.triggerCoins.Count > 0)
          {
              foreach (var coin in originalFsResult.triggerCoins)
              {
                  int row = coin.position[0];
                  int col = coin.position[1];
                  if (slotManager != null && slotManager.ResultMatrix != null && row < slotManager.ResultMatrix.Count)
                  {
                      var rowImages = slotManager.ResultMatrix[row].slotImages;
                      if (col < rowImages.Count)
                      {
                          var view = rowImages[col].GetComponent<SlotSymbolView>();
                          if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                          {
                              originalCoinWorldPositions.Add(view.losPolosValueText.transform.position);
                              originalCoinTexts.Add(view.losPolosValueText.text);
                              originalSourceTexts.Add(view.losPolosValueText);
                          }
                      }
                  }
              }
          }
          else
          {
              if (slotManager != null && slotManager.ResultMatrix != null)
              {
                  for (int r = 0; r < slotManager.ResultMatrix.Count; r++)
                  {
                      var rowImages = slotManager.ResultMatrix[r].slotImages;
                      for (int c = 0; c < rowImages.Count; c++)
                      {
                            var triggerResult = slotManager.OriginalFeatureTriggerResult;
                            if (triggerResult != null && triggerResult.matrix != null)
                            {
                                if (r < triggerResult.matrix.Count && c < triggerResult.matrix[r].Count)
                                {
                                    if (triggerResult.matrix[r][c] == "17")
                                    {
                                        var view = rowImages[c].GetComponent<SlotSymbolView>();
                                        if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                                        {
                                            originalCoinWorldPositions.Add(view.losPolosValueText.transform.position);
                                            originalCoinTexts.Add(view.losPolosValueText.text);
                                            originalSourceTexts.Add(view.losPolosValueText);
                                        }
                                    }
                                }
                            }
                      }
                  }
              }
          }

          List<TMP_Text> origTempTexts = new List<TMP_Text>();
          List<Vector3> origInitialLocalScales = new List<Vector3>();

          for (int i = 0; i < originalCoinWorldPositions.Count; i++)
          {
              TMP_Text tempText = Instantiate(losPolosTextPrefab, spawnParent);
              tempText.transform.position = originalCoinWorldPositions[i];
              tempText.text = originalCoinTexts[i];

              if (i < originalSourceTexts.Count && originalSourceTexts[i] != null)
              {
                  Vector3 sourceLossyScale = originalSourceTexts[i].transform.lossyScale;
                  Vector3 parentLossyScale = spawnParent.lossyScale;
                  Vector3 matchedLocalScale = new Vector3(
                      parentLossyScale.x != 0 ? sourceLossyScale.x / parentLossyScale.x : 1f,
                      parentLossyScale.y != 0 ? sourceLossyScale.y / parentLossyScale.y : 1f,
                      parentLossyScale.z != 0 ? sourceLossyScale.z / parentLossyScale.z : 1f
                  );
                  tempText.transform.localScale = matchedLocalScale;
                  origInitialLocalScales.Add(matchedLocalScale);

                  RectTransform rectTrans = tempText.GetComponent<RectTransform>();
                  RectTransform sourceRectTrans = originalSourceTexts[i].GetComponent<RectTransform>();
                  if (rectTrans != null && sourceRectTrans != null)
                  {
                      rectTrans.sizeDelta = sourceRectTrans.sizeDelta;
                  }

                  originalSourceTexts[i].gameObject.SetActive(false);
              }
              else
              {
                  origInitialLocalScales.Add(Vector3.one);
              }

              tempText.gameObject.SetActive(true);
              origTempTexts.Add(tempText);
          }

          yield return new WaitForSeconds(0.2f);

          foreach (var txt in origTempTexts)
          {
              txt.transform.DOMove(centerWorldPos, moveDuration).SetEase(Ease.OutQuad);
          }

          yield return new WaitForSeconds(moveDuration);

          if (midSumAnimObj != null) midSumAnimObj.SetActive(true);
          if (AudioController.Instance != null) AudioController.Instance.PlayFlyingTextSpark();

          TMP_Text origSumTextPrefab = null;
          Vector3 origSumInitialScale = Vector3.one;
          if (origTempTexts.Count > 0)
          {
              origSumTextPrefab = origTempTexts[0];
              origSumInitialScale = origInitialLocalScales[0];
              string sumSpriteText = "<sprite=10>";
              string originalSpinsStr = originalFreeSpins.ToString();
              foreach (char c in originalSpinsStr)
              {
                  if (char.IsDigit(c))
                  {
                      sumSpriteText += $"<sprite={c - '0'}>";
                  }
              }
              origSumTextPrefab.text = sumSpriteText;

              for (int i = 1; i < origTempTexts.Count; i++)
              {
                  if (origTempTexts[i] != null) Destroy(origTempTexts[i].gameObject);
              }
              origTempTexts.Clear();
          }

          if (origSumTextPrefab != null)
          {
              origSumTextPrefab.transform.DOScale(origSumInitialScale * 1.8f, 0.4f).SetEase(Ease.OutBack);
              yield return new WaitForSeconds(0.4f);
              origSumTextPrefab.transform.DOScale(origSumInitialScale * 1.4f, 0.3f).SetEase(Ease.InQuad);
              yield return new WaitForSeconds(0.3f);

              yield return new WaitForSeconds(0.5f);

              Vector3 targetPos = spinCounterPanel != null ? spinCounterPanel.transform.position : centerWorldPos;
              if (spinCountSnapParent != null)
              {
                  targetPos = spinCountSnapParent.position;
              }

              if (midSumAnimObj != null) midSumAnimObj.SetActive(false);

              origSumTextPrefab.transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutQuad);
              yield return new WaitForSeconds(moveDuration);

              if (dissolveAnimObj != null) dissolveAnimObj.SetActive(true);
              if (AudioController.Instance != null) AudioController.Instance.PlayFlyingTextSpark();

              if (spinCounterText != null)
              {
                  int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
                  int spinsUsed = totalFreeSpins - remaining;
                  spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
                  spinCounterText.gameObject.SetActive(true);
              }
              Destroy(origSumTextPrefab.gameObject);

              yield return new WaitForSeconds(1.0f);
              if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);
          }
          else
          {
              if (spinCounterText != null)
              {
                  int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
                  int spinsUsed = totalFreeSpins - remaining;
                  spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
                  spinCounterText.gameObject.SetActive(true);
              }
          }
      }

      if (!isRetrigger)
      {
          yield return new WaitForSeconds(0.5f);

          if (DialoguePopupManager.Instance != null)
          {
              yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.FreeSpinStart);
          }

          bool popupClicked = false;
          OpenFeaturePopup(() => {
              popupClicked = true;
          });

          yield return new WaitUntil(() => popupClicked);

            if (featureWinText != null)
            {
                if (fromBonusSlot && accumulatedFreeSpinWin > 0)
                {
                    featureWinText.text = FormatSpriteText(FormatStaticValue(accumulatedFreeSpinWin));
                }
                else
                {
                    featureWinText.text = FormatSpriteText("00.00");
                }
                featureWinText.gameObject.SetActive(true);
            }
      }
  }
}

public class DropdownItemDisabler : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [SerializeField] internal List<int> indexesToDisable = new List<int>();
    [SerializeField] internal int SelectedIndexOverride = -1;

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        SelectedIndexOverride = -1;
        StartCoroutine(DisableItemsCoroutine());
    }

    private IEnumerator DisableItemsCoroutine()
    {
        yield return null;

        var dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null) yield break;

        Transform dropdownList = dropdown.transform.Find("Dropdown List");
        if (dropdownList == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                dropdownList = canvas.transform.Find("Dropdown List");
            }
        }

        if (dropdownList == null)
        {
            var go = GameObject.Find("Dropdown List");
            if (go != null)
            {
                dropdownList = go.transform;
            }
        }

        if (dropdownList != null)
        {
            Transform content = dropdownList.Find("Viewport/Content");
            if (content != null)
            {
                List<Toggle> optionToggles = new List<Toggle>();
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child.gameObject.activeSelf)
                    {
                        Toggle toggle = child.GetComponent<Toggle>();
                        if (toggle != null)
                        {
                            optionToggles.Add(toggle);
                        }
                    }
                }

                for (int i = 0; i < optionToggles.Count; i++)
                {
                    int index = i;
                    optionToggles[i].onValueChanged.AddListener((isOn) => {
                        if (isOn) {
                            SelectedIndexOverride = index;
                            StartCoroutine(SetDropdownValueCoroutine(dropdown, index));
                        }
                    });

                    if (indexesToDisable.Contains(i))
                    {
                        optionToggles[i].interactable = false;
                        var graphics = optionToggles[i].GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                        foreach (var graphic in graphics)
                        {
                            graphic.enabled = false;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator SetDropdownValueCoroutine(TMP_Dropdown dropdown, int index)
    {
        yield return null;
        dropdown.value = index;
    }
}
