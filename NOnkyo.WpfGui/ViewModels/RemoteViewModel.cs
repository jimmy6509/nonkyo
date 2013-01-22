﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NOnkyo.ISCP;
using NOnkyo.WpfGui.ViewModels.Commands;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Drawing;

namespace NOnkyo.WpfGui.ViewModels
{
    public class RemoteViewModel : ViewModelBase, IDisposable
    {
        #region Event SelectDevice

        [NonSerialized()]
        private EventHandler<SelectDeviceEventArgs> EventSelectDevice;
        public event EventHandler<SelectDeviceEventArgs> SelectDevice
        {
            add
            { this.EventSelectDevice += value; }
            remove
            { this.EventSelectDevice -= value; }
        }

        protected virtual void OnSelectDevice(SelectDeviceEventArgs e)
        {
            EventHandler<SelectDeviceEventArgs> loHandler = this.EventSelectDevice;
            if (loHandler != null)
                loHandler(this, e);
        }

        #endregion

        #region Event CloseInputSelector

        [NonSerialized()]
        private EventHandler EventCloseInputSelector;
        public event EventHandler CloseInputSelector
        {
            add
            { this.EventCloseInputSelector += value; }
            remove
            { this.EventCloseInputSelector -= value; }
        }

        protected virtual void OnCloseInputSelector()
        {
            EventHandler loHandler = this.EventCloseInputSelector;
            if (loHandler != null)
                loHandler(this, EventArgs.Empty);
        }

        #endregion

        #region Event CloseInputSelector

        [NonSerialized()]
        private EventHandler<KeyboardInputEventArgs> EventKeyboardInput;
        public event EventHandler<KeyboardInputEventArgs> KeyboardInput
        {
            add
            { this.EventKeyboardInput += value; } 
            remove
            { this.EventKeyboardInput -= value; } 
        }

        protected virtual void OnKeyboardInput(KeyboardInputEventArgs e)
        {
            EventHandler<KeyboardInputEventArgs> loHandler = this.EventKeyboardInput;
            if (loHandler != null)
                loHandler(this, e);
        }

        #endregion

        #region Attributes

        private Device moCurrentDevice;
        private string msConnectState;
        private ObservableCollection<string> moLogList = new ObservableCollection<string>();
        private IConnection moConnection;
        private TaskScheduler moUITaskScheduler;

        private int mnSelectedTabIndex = 0;
        private int mnCurrentVolume;
        private string msCurrentInputSelector;
        private string msCurrentListeningMode;
        private bool? mbMuteStatus = null;
        private bool mbShowNetItems;
        private List<NetListItem> moNetItemList;
        private NetListItem moSelectedNetItem;
        private string msCurrentNetworkGuiTitle;
        private string msNetAlbumName;
        private string msNetArtistName;
        private string msNetTimeInfo;
        private string msNetTitleName;
        private string msNetTrackInfo;
        private bool mbShowNetPlayStatus;
        private EPlayStatus mePlayStatus;
        private ERepeatStatus meRepeatStatus;
        private EShuffleStatus meShuffleStatus;
        private Byte[] moAlbumImage;
        private bool mbIsPowerOn;

        #endregion

        #region Commands

        #region ShowHome

        private RelayCommand moShowHomeCommand;
        public ICommand ShowHomeCommand
        {
            get
            {
                if (this.moShowHomeCommand == null)
                    this.moShowHomeCommand = new RelayCommand(param => this.ShowHome(),
                        param => this.CanShowHome());
                return this.moShowHomeCommand;
            }
        }

        private void ShowHome()
        {
            this.ChoseImputSelector(this.GetCommand<ISCP.Command.InputSelector>().CurrentInputSelector);
        }


        private bool CanShowHome()
        {
            return true;
        }

        #endregion

        #region ShowAudio

        private RelayCommand moShowAudioCommand;
        public ICommand ShowAudioCommand
        {
            get
            {
                if (this.moShowAudioCommand == null)
                    this.moShowAudioCommand = new RelayCommand(param => this.ShowAudio(),
                        param => this.CanShowAudio());
                return this.moShowAudioCommand;
            }
        }

        private void ShowAudio()
        {
            this.SelectedTabIndex = 1;
        }


        private bool CanShowAudio()
        {
            return true;
        }

        #endregion

        #region ShowRescan

        private RelayCommand moShowRescanCommand;
        public ICommand ShowRescanCommand
        {
            get
            {
                if (this.moShowRescanCommand == null)
                    this.moShowRescanCommand = new RelayCommand(param => this.ShowRescan(),
                        param => this.CanShowRescan());
                return this.moShowRescanCommand;
            }
        }

        private void ShowRescan()
        {
            var loArgs = new SelectDeviceEventArgs();
            this.OnSelectDevice(loArgs);
            if (loArgs.Device == null)
                IsDialogClose = true;
            else
            {
                this.CurrentDevice = loArgs.Device;
                this.ConnectState = "Establish a connection to " + this.CurrentDevice.ToString() + " ....";
                this.OpenConnection();
            }
        }

        private bool CanShowRescan()
        {
            return true;
        }

        #endregion

        #region VolumeUp

        private RelayCommand moVolumeUpCommand;
        public ICommand VolumeUpCommand
        {
            get
            {
                if (this.moVolumeUpCommand == null)
                    this.moVolumeUpCommand = new RelayCommand(param => this.VolumeUp(),
                        param => this.CanVolumeUp());
                return this.moVolumeUpCommand;
            }
        }

        private void VolumeUp()
        {
            this.moConnection.SendCommand(ISCP.Command.MasterVolume.UP);
        }

        private bool CanVolumeUp()
        {
            return true;
        }

        #endregion

        #region VolumeDown

        private RelayCommand moVolumeDownCommand;
        public ICommand VolumeDownCommand
        {
            get
            {
                if (this.moVolumeDownCommand == null)
                    this.moVolumeDownCommand = new RelayCommand(param => this.VolumeDown(),
                        param => this.CanVolumeDown());
                return this.moVolumeDownCommand;
            }
        }

        private void VolumeDown()
        {
            this.moConnection.SendCommand(ISCP.Command.MasterVolume.DOWN);
        }

        private bool CanVolumeDown()
        {
            return true;
        }

        #endregion

        #region MuteToggle

        private RelayCommand moMuteToggleCommand;
        public ICommand MuteToggleCommand
        {
            get
            {
                if (this.moMuteToggleCommand == null)
                    this.moMuteToggleCommand = new RelayCommand(param => this.MuteToggle(),
                        param => this.CanMuteToggle());
                return this.moMuteToggleCommand;
            }
        }

        private void MuteToggle()
        {
            this.moConnection.SendCommand(ISCP.Command.AudioMuting.Chose(!this.mbMuteStatus.GetValueOrDefault(), this.CurrentDevice));
        }

        private bool CanMuteToggle()
        {
            return true;
        }

        #endregion

        #region InputSelector

        private RelayCommand moInputSelectorCommand;
        public ICommand InputSelectorCommand
        {
            get
            {
                if (this.moInputSelectorCommand == null)
                    this.moInputSelectorCommand = new RelayCommand(param => this.InputSelector(param),
                        param => this.CanInputSelector(param));
                return this.moInputSelectorCommand;
            }
        }

        private void InputSelector(object poParam)
        {
            EInputSelector leInputSelector = poParam.ToString().ToEnum<EInputSelector>();
            this.moConnection.SendCommand(ISCP.Command.InputSelector.Chose(leInputSelector, this.CurrentDevice));
            this.OnCloseInputSelector();
            this.ChoseImputSelector(leInputSelector);
        }

        private bool CanInputSelector(object poParam)
        {
            return true;
        }

        #endregion

        #region ListeningModeMovieUP

        private RelayCommand moListeningModeMovieUPCommand;
        public ICommand ListeningModeMovieUPCommand
        {
            get
            {
                if (this.moListeningModeMovieUPCommand == null)
                    this.moListeningModeMovieUPCommand = new RelayCommand(param => this.ListeningModeMovieUP(),
                        param => this.CanListeningModeMovieUP());
                return this.moListeningModeMovieUPCommand;
            }
        }

        private void ListeningModeMovieUP()
        {
            this.moConnection.SendCommand(ISCP.Command.ListeningMode.MovieUP);
        }

        private bool CanListeningModeMovieUP()
        {
            return true;
        }

        #endregion

        #region ListeningModeMusicUP

        private RelayCommand moListeningModeMusicUPCommand;
        public ICommand ListeningModeMusicUPCommand
        {
            get
            {
                if (this.moListeningModeMusicUPCommand == null)
                    this.moListeningModeMusicUPCommand = new RelayCommand(param => this.ListeningModeMusicUP(),
                        param => this.CanListeningModeMusicUP());
                return this.moListeningModeMusicUPCommand;
            }
        }

        private void ListeningModeMusicUP()
        {
            this.moConnection.SendCommand(ISCP.Command.ListeningMode.MusicUP);
        }

        private bool CanListeningModeMusicUP()
        {
            return true;
        }

        #endregion

        #region ListeningModeGameUP

        private RelayCommand moListeningModeGameUPCommand;
        public ICommand ListeningModeGameUPCommand
        {
            get
            {
                if (this.moListeningModeGameUPCommand == null)
                    this.moListeningModeGameUPCommand = new RelayCommand(param => this.ListeningModeGameUP(),
                        param => this.CanListeningModeGameUP());
                return this.moListeningModeGameUPCommand;
            }
        }

        private void ListeningModeGameUP()
        {
            this.moConnection.SendCommand(ISCP.Command.ListeningMode.GameUP);
        }

        private bool CanListeningModeGameUP()
        {
            return true;
        }

        #endregion

        #region SetupOperation

        private RelayCommand moSetupOperationCommand;
        public ICommand SetupOperationCommand
        {
            get
            {
                if (this.moSetupOperationCommand == null)
                    this.moSetupOperationCommand = new RelayCommand(param => this.SetupOperation(param),
                        param => this.CanSetupOperation(param));
                return this.moSetupOperationCommand;
            }
        }


        private void SetupOperation(object poParam)
        {
            this.moConnection.SendCommand(ISCP.Command.OSD.Chose(poParam.ToString().ToEnum<EOSDOperation>(), this.CurrentDevice));
        }

        private bool CanSetupOperation(object poParam)
        {
            return true;
        }

        #endregion

        #region Preset

        private RelayCommand moPresetCommand;
        public ICommand PresetCommand
        {
            get
            {
                if (this.moPresetCommand == null)
                    this.moPresetCommand = new RelayCommand(param => this.Preset(param),
                        param => this.CanPreset(param));
                return this.moPresetCommand;
            }
        }

        private void Preset(object poParam)
        {
            this.moConnection.SendCommand(ISCP.Command.Preset.Chose(Convert.ToInt32(poParam), this.CurrentDevice));
        }

        private bool CanPreset(object poParam)
        {
            return true;
        }

        #endregion

        #region PresetUp

        private RelayCommand moPresetUpCommand;
        public ICommand PresetUpCommand
        {
            get
            {
                if (this.moPresetUpCommand == null)
                    this.moPresetUpCommand = new RelayCommand(param => this.PresetUp(),
                        param => this.CanPresetUp());
                return this.moPresetUpCommand;
            }
        }

        private void PresetUp()
        {
            this.moConnection.SendCommand(ISCP.Command.Preset.Up);
        }

        private bool CanPresetUp()
        {
            return true;
        }

        #endregion

        #region PresetDown

        private RelayCommand moPresetDownCommand;
        public ICommand PresetDownCommand
        {
            get
            {
                if (this.moPresetDownCommand == null)
                    this.moPresetDownCommand = new RelayCommand(param => this.PresetDown(),
                        param => this.CanPresetDown());
                return this.moPresetDownCommand;
            }
        }

        private void PresetDown()
        {
            this.moConnection.SendCommand(ISCP.Command.Preset.Down);
        }

        private bool CanPresetDown()
        {
            return true;
        }

        #endregion

        #region NetTune

        private RelayCommand moNetTuneCommand;
        public ICommand NetTuneCommand
        {
            get
            {
                if (this.moNetTuneCommand == null)
                    this.moNetTuneCommand = new RelayCommand(param => this.NetTune(param),
                        param => this.CanNetTune(param));
                return this.moNetTuneCommand;
            }
        }

        private void NetTune(object poParam)
        {
            ENetTuneOperation leNetTuneOperation = poParam.ToString().ToEnum<ENetTuneOperation>();
            if (leNetTuneOperation == ENetTuneOperation.SELECT && this.moSelectedNetItem != null)
            {
                this.moConnection.SendCommand(ISCP.Command.NetListInfo.ChoseLine(this.moSelectedNetItem.Line, this.CurrentDevice));
            }
            else
                this.moConnection.SendCommand(ISCP.Command.NetTune.Chose(leNetTuneOperation, this.CurrentDevice));
        }

        private bool CanNetTune(object poParam)
        {
            return true;
        }

        #endregion

        #region SelectNetItem

        private RelayCommand moSelectNetItemCommand;
        public ICommand SelectNetItemCommand
        {
            get
            {
                if (this.moSelectNetItemCommand == null)
                    this.moSelectNetItemCommand = new RelayCommand(param => this.SelectNetItem(),
                        param => this.CanSelectNetItem());
                return this.moSelectNetItemCommand;
            }
        }

        private void SelectNetItem()
        {
            if (this.moSelectedNetItem != null)
            {
                this.moConnection.SendCommand(ISCP.Command.NetListInfo.ChoseLine(this.moSelectedNetItem.Line, this.CurrentDevice));
            }
        }

        private bool CanSelectNetItem()
        {
            return true;
        }

        #endregion

        #region Power

        private RelayCommand moPowerCommand;
        public ICommand PowerCommand
        {
            get
            {
                if (this.moPowerCommand == null)
                    this.moPowerCommand = new RelayCommand(param => this.Power(),
                        param => this.CanPower());
                return this.moPowerCommand;
            }
        }

        private void Power()
        {
            if (this.IsPowerOn)
                this.moConnection.SendCommand(ISCP.Command.Power.Off(this.CurrentDevice));
            else
            {
                this.moConnection.SendCommand(ISCP.Command.Power.On(this.CurrentDevice));
                System.Threading.Thread.Sleep(1000);
                this.QueryStatusInfos();
            }
        }

        private bool CanPower()
        {
            return true;
        }

        #endregion

        #endregion

        #region Public Methods / Properties

        public int SelectedTabIndex
        {
            get { return this.mnSelectedTabIndex; }
            set
            {
                if (this.mnSelectedTabIndex != value)
                {
                    this.mnSelectedTabIndex = value;
                    this.OnPropertyChanged(() => this.SelectedTabIndex);
                }
            }
        }

        public int CurrentVolume
        {
            get { return this.mnCurrentVolume; }
            set
            {
                if (this.mnCurrentVolume != value)
                {
                    System.Diagnostics.Debug.WriteLine(value.ToString());
                    this.mnCurrentVolume = value;
                    this.OnPropertyChanged(() => this.CurrentVolume);
                }
            }
        }

        public bool? MuteStatus
        {
            get { return this.mbMuteStatus; }
            set
            {
                if (this.mbMuteStatus != value)
                {
                    this.mbMuteStatus = value;
                    this.OnPropertyChanged(() => this.MuteStatus);
                }
            }
        }

        public string CurrentInputSelector
        {
            get { return this.msCurrentInputSelector; }
            set
            {
                if (this.msCurrentInputSelector != value)
                {
                    this.msCurrentInputSelector = value;
                    this.OnPropertyChanged(() => this.CurrentInputSelector);
                }
            }
        }

        public string CurrentListeningMode
        {
            get { return this.msCurrentListeningMode; }
            set
            {
                if (this.msCurrentListeningMode != value)
                {
                    this.msCurrentListeningMode = value;
                    this.OnPropertyChanged(() => this.CurrentListeningMode);
                }
            }
        }

        public Device CurrentDevice
        {
            get
            {
                return this.moCurrentDevice;
            }
            set
            {
                if (this.moCurrentDevice != value)
                {
                    this.moCurrentDevice = value;
                    this.OnPropertyChanged(() => this.CurrentDevice);
                }
            }
        }

        public string ConnectState
        {
            get
            {
                return this.msConnectState;
            }
            set
            {
                if (this.msConnectState != value)
                {
                    this.msConnectState = value;
                    this.OnPropertyChanged(() => this.ConnectState);
                }
            }
        }

        public ObservableCollection<string> LogList
        {
            get
            {
                return this.moLogList;
            }
            set
            {
                if (this.moLogList != value)
                {
                    this.moLogList = value;
                    this.OnPropertyChanged(() => this.LogList);
                }
            }
        }

        public bool ShowNetItems
        {
            get { return this.mbShowNetItems; }
            set
            {
                if (this.mbShowNetItems != value)
                {
                    this.mbShowNetItems = value;
                    this.OnPropertyChanged(() => this.ShowNetItems);
                }
            }
        }

        public bool ShowNetPlayStatus
        {
            get { return this.mbShowNetPlayStatus; }
            set
            {
                if (this.mbShowNetPlayStatus != value)
                {
                    this.mbShowNetPlayStatus = value;
                    this.OnPropertyChanged(() => this.ShowNetPlayStatus);
                }
            }
        }

        public List<NetListItem> NetItemList
        {
            get
            {
                return this.moNetItemList;
            }
            private set
            {
                if (this.moNetItemList != value)
                {
                    this.moNetItemList = value;
                    this.OnPropertyChanged(() => this.NetItemList);
                }
            }
        }

        public NetListItem SelectedNetItem
        {
            get { return this.moSelectedNetItem; }
            set
            {
                if (this.moSelectedNetItem != value)
                {
                    this.moSelectedNetItem = value;
                    this.OnPropertyChanged(() => this.SelectedNetItem);
                }
            }
        }

        public string CurrentNetworkGuiTitle
        {
            get { return this.msCurrentNetworkGuiTitle; }
            set
            {
                if (this.msCurrentNetworkGuiTitle != value)
                {
                    this.msCurrentNetworkGuiTitle = value;
                    this.OnPropertyChanged(() => this.CurrentNetworkGuiTitle);
                }
            }
        }

        public string NetAlbumName
        {
            get { return this.msNetAlbumName; }
            set
            {
                if (this.msNetAlbumName != value)
                {
                    this.msNetAlbumName = value;
                    this.OnPropertyChanged(() => this.NetAlbumName);
                }
            }
        }

        public string NetArtistName
        {
            get { return this.msNetArtistName; }
            set
            {
                if (this.msNetArtistName != value)
                {
                    this.msNetArtistName = value;
                    this.OnPropertyChanged(() => this.NetArtistName);
                }
            }
        }

        public string NetTimeInfo
        {
            get { return this.msNetTimeInfo; }
            set
            {
                if (this.msNetTimeInfo != value)
                {
                    this.msNetTimeInfo = value;
                    this.OnPropertyChanged(() => this.NetTimeInfo);
                }
            }
        }

        public string NetTitleName
        {
            get { return this.msNetTitleName; }
            set
            {
                if (this.msNetTitleName != value)
                {
                    this.msNetTitleName = value;
                    this.OnPropertyChanged(() => this.NetTitleName);
                }
            }
        }

        public string NetTrackInfo
        {
            get { return this.msNetTrackInfo; }
            set
            {
                if (this.msNetTrackInfo != value)
                {
                    this.msNetTrackInfo = value;
                    this.OnPropertyChanged(() => this.NetTrackInfo);
                }
            }
        }

        public EPlayStatus PlayStatus
        {
            get { return this.mePlayStatus; }
            set
            {
                if (this.mePlayStatus != value)
                {
                    this.mePlayStatus = value;
                    this.OnPropertyChanged(() => this.PlayStatus);
                }
            }
        }

        public ERepeatStatus RepeatStatus
        {
            get { return this.meRepeatStatus; }
            set
            {
                if (this.meRepeatStatus != value)
                {
                    this.meRepeatStatus = value;
                    this.OnPropertyChanged(() => this.RepeatStatus);
                }
            }
        }

        public EShuffleStatus ShuffleStatus
        {
            get { return this.meShuffleStatus; }
            set
            {
                if (this.meShuffleStatus != value)
                {
                    this.meShuffleStatus = value;
                    this.OnPropertyChanged(() => this.ShuffleStatus);
                }
            }
        }

        public void WindowLoaded()
        {
            this.ShowRescanCommand.Execute(null);
        }

        public Byte[] AlbumImage
        {
            get { return this.moAlbumImage; }
            set
            {
                if (this.moAlbumImage != value)
                {
                    this.moAlbumImage = value;
                    this.OnPropertyChanged(() => this.AlbumImage);
                }
            }
        }

        public bool IsPowerOn
        {
            get { return this.mbIsPowerOn; }
            set
            {
                if (this.mbIsPowerOn != value)
                {
                    this.mbIsPowerOn = value;
                    this.OnPropertyChanged(() => this.IsPowerOn);
                }
            }
        }

        #endregion

        #region Private Methods / Properties

        private void OpenConnection()
        {
            this.CloseConnection();
            this.moUITaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            this.LogList = new ObservableCollection<string>();
            this.moConnection = App.Container.Resolve<IConnection>();
            var lbSuccess = this.moConnection.Connect(this.moCurrentDevice);

            if (lbSuccess)
            {
                this.ConnectState = "Connect to " + this.CurrentDevice.ToString();
                this.moConnection.MessageReceived += new EventHandler<MessageReceivedEventArgs>(Connection_MessageReceived);
                this.moConnection.ConnectionClosed += new EventHandler(Connection_ConnectionClosed);
            }
            else
                throw new ApplicationException("Cannot connect to Receiver: {0} with Address {1}:{2}".FormatWith(this.moCurrentDevice.Model, this.moCurrentDevice.IP, this.moCurrentDevice.Port));

            this.QueryStatusInfos();
        }

        private void QueryStatusInfos()
        {
            this.moConnection.SendCommand(ISCP.Command.Power.State);
            this.moConnection.SendCommand(ISCP.Command.MasterVolume.State);
            this.moConnection.SendCommand(ISCP.Command.InputSelector.State);
            this.moConnection.SendCommand(ISCP.Command.ListeningMode.State);
            this.moConnection.SendCommand(ISCP.Command.AudioMuting.State);
        }

        private void CloseConnection()
        {
            if (this.moConnection != null)
                this.moConnection.Dispose();
        }

        private void ChoseImputSelector(EInputSelector peInputSelector)
        {
            this.ShowNetItems =
                this.ShowNetPlayStatus = false;
            this.NetAlbumName =
                this.NetArtistName =
                this.NetTimeInfo =
                this.NetTrackInfo = string.Empty;
            this.AlbumImage = null;
            switch (peInputSelector)
            {
                case EInputSelector.VIDEO1:
                    break;
                case EInputSelector.VIDEO2:
                    break;
                case EInputSelector.VIDEO3:
                    break;
                case EInputSelector.VIDEO4:
                    break;
                case EInputSelector.VIDEO5:
                    break;
                case EInputSelector.VIDEO6:
                    break;
                case EInputSelector.VIDEO7:
                    break;
                case EInputSelector.HIDDEN1:
                    break;
                case EInputSelector.HIDDEN2:
                    break;
                case EInputSelector.HIDDEN3:
                    break;
                case EInputSelector.BDDVD:
                    break;
                case EInputSelector.TAPE1:
                    break;
                case EInputSelector.TAPE2:
                    break;
                case EInputSelector.PHONO:
                    break;
                case EInputSelector.TVCD:
                    break;
                case EInputSelector.FM:
                case EInputSelector.AM:
                case EInputSelector.TUNER:
                    this.SelectedTabIndex = 0;
                    break;
                case EInputSelector.MUSICSERVER:
                case EInputSelector.NETWORKNET:
                case EInputSelector.INTERNETRADIO:
                case EInputSelector.USB:
                case EInputSelector.USBREAR:
                case EInputSelector.USBTOGGLE:
                    this.SelectedTabIndex = 1;
                    this.ShowNetItems = true;
                    this.ShowNetPlayStatus = true;
                    break;
                case EInputSelector.UNIVERSALPORT:
                    break;
                case EInputSelector.MULTICH:
                    break;
                case EInputSelector.XM:
                    break;
                case EInputSelector.SIRIUS:
                    break;
                default:
                    break;
            }
        }

        private T GetCommand<T>() where T : ISCP.Command.CommandBase
        {
            return ISCP.Command.CommandBase.CommandList.First(item => item.GetType() == typeof(T)) as T;
        }

        private void TaskError(Task poTask)
        {
            if (poTask.Exception != null)
            {
                //this.ErrorCatcher(poTask, poTask.Exception.InnerException);
            }
        }

        #endregion

        #region EventHandler

        private void Connection_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var loCommand in ISCP.Command.CommandBase.CommandList.Where(item => item.Match(e.Message)))
                {
                    if (loCommand is ISCP.Command.Power)
                    {
                        this.IsPowerOn = (loCommand as ISCP.Command.Power).IsOn;
                    }

                    if (loCommand is ISCP.Command.MasterVolume)
                    {
                        this.CurrentVolume = (loCommand as ISCP.Command.MasterVolume).VolumeLevel;
                    }

                    if (loCommand is ISCP.Command.InputSelector)
                    {
                        EInputSelector leInputSelector = (loCommand as ISCP.Command.InputSelector).CurrentInputSelector;
                        this.CurrentInputSelector = leInputSelector.ToDescription();
                        this.ChoseImputSelector(leInputSelector);
                    }

                    if (loCommand is ISCP.Command.ListeningMode)
                    {
                        this.CurrentListeningMode = (loCommand as ISCP.Command.ListeningMode).CurrentListeningMode.ToDescription();
                    }

                    if (loCommand is ISCP.Command.AudioMuting)
                    {
                        this.MuteStatus = (loCommand as ISCP.Command.AudioMuting).Mute;
                    }

                    if (loCommand is ISCP.Command.NetListInfo)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetListInfo);
                        this.NetItemList = new List<NetListItem>(loCurrentCommand.InfoList);
                        this.moSelectedNetItem = this.NetItemList.FirstOrDefault(item => item.Line == loCurrentCommand.CursorPosition);
                        this.ShowNetItems = this.NetItemList.Count > 0;
                        this.OnPropertyChanged(() => this.SelectedNetItem);
                    }

                    if (loCommand is ISCP.Command.NLT)
                    {
                        this.CurrentNetworkGuiTitle = (loCommand as ISCP.Command.NLT).CurrentTitle;
                    }

                    if (loCommand is ISCP.Command.NetAlbumName)
                    {
                        this.NetAlbumName = (loCommand as ISCP.Command.NetAlbumName).Info;
                    }

                    if (loCommand is ISCP.Command.NetArtistName)
                    {
                        this.NetArtistName = (loCommand as ISCP.Command.NetArtistName).Info;
                    }

                    if (loCommand is ISCP.Command.NetTitleName)
                    {
                        this.NetTitleName = (loCommand as ISCP.Command.NetTitleName).Info;
                    }

                    if (loCommand is ISCP.Command.NetTimeInfo)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetTimeInfo);
                        if (loCurrentCommand.IsComplete)
                            this.NetTimeInfo = "Time: {0} / {1}".FormatWith(loCurrentCommand.Elapsed, loCurrentCommand.Total);
                        else
                            this.NetTimeInfo = string.Empty;
                    }

                    if (loCommand is ISCP.Command.NetTrackInfo)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetTrackInfo);
                        this.NetTrackInfo = "Track: {0} / {1}".FormatWith(loCurrentCommand.CurrentTrack, loCurrentCommand.TotalTrack);
                    }

                    if (loCommand is ISCP.Command.NetPlayStatus)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetPlayStatus);
                        this.PlayStatus = loCurrentCommand.PlayStatus;
                        this.RepeatStatus = loCurrentCommand.RepeatStatus;
                        this.ShuffleStatus = loCurrentCommand.ShuffleStatus;
                        this.ShowNetPlayStatus = true;
                    }

                    if (loCommand is ISCP.Command.NetJacketArt)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetJacketArt);
                        if (loCurrentCommand.IsReady)
                            this.AlbumImage = loCurrentCommand.Album;
                        else
                            this.AlbumImage = null;
                    }

                    if (loCommand is ISCP.Command.NetKeyboard)
                    {
                        var loCurrentCommand = (loCommand as ISCP.Command.NetKeyboard);
                        KeyboardInputEventArgs loArgs = null;

                        switch (loCurrentCommand.Category)
                        {
                            case EKeyboardCategory.OFF:
                                loArgs = new KeyboardInputEventArgs(string.Empty, true);
                                break;
                            case EKeyboardCategory.USERNAME:
                                loArgs = new KeyboardInputEventArgs("User Name", false);
                                break;
                            case EKeyboardCategory.PASSWORD:
                                loArgs = new KeyboardInputEventArgs("Password", false);
                                break;
                            case EKeyboardCategory.ARTISTNAME:
                                loArgs = new KeyboardInputEventArgs("Artist", false);
                                break;
                            case EKeyboardCategory.ALBUMNAME:
                                loArgs = new KeyboardInputEventArgs("Album", false);
                                break;
                            case EKeyboardCategory.SONGNAME:
                                loArgs = new KeyboardInputEventArgs("Song", false);
                                break;
                            case EKeyboardCategory.STATIONNAME:
                                loArgs = new KeyboardInputEventArgs("Station", false);
                                break;
                            case EKeyboardCategory.TAGNAME:
                                loArgs = new KeyboardInputEventArgs("Tag", false);
                                break;
                            case EKeyboardCategory.ARTISTORSONG:
                                loArgs = new KeyboardInputEventArgs("Artist or Song", false);
                                break;
                            case EKeyboardCategory.EPISODENAME:
                                loArgs = new KeyboardInputEventArgs("Episode", false);
                                break;
                            case EKeyboardCategory.PINCODE:
                                loArgs = new KeyboardInputEventArgs("Pincode", false);
                                break;
                            case EKeyboardCategory.ACCESSNAME:
                                loArgs = new KeyboardInputEventArgs("Accessname", false);
                                break;
                            case EKeyboardCategory.ACCESSPASSWORD:
                                loArgs = new KeyboardInputEventArgs("Accesspassword", false);
                                break;
                        }

                        if (loArgs != null)
                        {
                            loArgs.Category = loCurrentCommand.Category;
                            this.OnKeyboardInput(loArgs);
                            if (!loArgs.CloseInputView)
                            {
                                if (loArgs.Input.IsEmpty())
                                    this.moConnection.SendCommand(ISCP.Command.NetTune.Chose(ENetTuneOperation.RETURN, this.CurrentDevice));
                                else
                                    this.moConnection.SendCommand( ISCP.Command.NetKeyboard.Send(loArgs.Input, this.CurrentDevice));
                            }
                        }
                    }
                }
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, this.moUITaskScheduler)
            .ContinueWith(t => this.TaskError(t), this.moUITaskScheduler);
        }

        public void Connection_ConnectionClosed(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                this.CloseConnection();
                this.ConnectState = "Connection closed";
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, this.moUITaskScheduler);
        }

        #endregion

        #region IDisposable

        // Track whether Dispose has been called.
        private bool mbDisposed = false;

        /// <summary>
        /// Implement IDisposable
        /// Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing">true, to dispose managed ad unmanaged resources, false to dispose unmanaged resources only</param>
        protected virtual void Dispose(bool disposing)
        {
            // Note that this is not thread safe.
            // Another thread could start disposing the object
            // after the managed resources are disposed,
            // but before the disposed flag is set to true.
            // If thread safety is necessary, it must be
            // implemented by the client.

            // Check to see if Dispose has already been called.
            if (!this.mbDisposed)
            {
                try
                {
                    // If disposing equals true, dispose all managed 
                    // and unmanaged resources.
                    if (disposing)
                    {
                        // Dispose managed resources. HERE ->
                        this.CloseConnection();

                        //Release ComObjects
                        //System.Runtime.InteropServices.Marshal.ReleaseComObject(ComObj);
                    }
                    // Release unmanaged resources. If disposing is false, 
                    // only the following code is executed. HERE ->
                }
                catch (Exception)
                {
                    this.mbDisposed = false;
                    throw;
                }
                this.mbDisposed = true;
            }
        }

        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method 
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~RemoteViewModel()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            this.Dispose(false);
        }

        #endregion IDisposable
    }
}