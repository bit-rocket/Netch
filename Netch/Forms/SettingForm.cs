using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using Netch.ViewModels;
using ReactiveUI;

namespace Netch.Forms
{
    public partial class SettingForm : Form, IViewFor<Setting>
    {
        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new();

        private readonly Dictionary<Control, Action<Control>> _saveActions = new();

        public SettingForm()
        {
            InitializeComponent();
            Icon = Resources.icon;
            i18N.TranslateForm(this);

            ViewModel = Global.Settings.Clone();

            #region General

            ushort CheckPorts(Control sender, ushort defaultValue)
            {
                var http = HTTPPortTextBox;
                var socks = Socks5PortTextBox;
                if (http.Text == socks.Text)
                {
                    Utils.Utils.ChangeControlForeColor(http, Color.Red);
                    Utils.Utils.ChangeControlForeColor(socks, Color.Red);
                    return defaultValue;
                }

                return ushort.TryParse(sender.Text, out var p) ? p : defaultValue;
            }

            this.WhenActivated(disposable =>
            {
                // General
                this.Bind(ViewModel,
                        vm => vm.Socks5LocalPort,
                        v => v.Socks5PortTextBox.Text,
                        vm => vm.ToString(),
                        _ => CheckPorts(Socks5PortTextBox, 2801))
                    .DisposeWith(disposable);

                this.Bind(ViewModel, vm => vm.HTTPLocalPort, v => v.HTTPPortTextBox.Text, vm => vm.ToString(), _ => CheckPorts(HTTPPortTextBox, 2802))
                    .DisposeWith(disposable);

                this.Bind(ViewModel,
                        vm => vm.LocalAddress,
                        v => v.AllowDevicesCheckBox.Checked,
                        vmToViewConverterOverride: BoolLocalAddressConverter.LocalAddressToBoolConverter.Instance,
                        viewToVMConverterOverride: BoolLocalAddressConverter.BoolToLocalAddressConverter.Instance)
                    .DisposeWith(disposable);

                this.Bind(ViewModel, vm => vm.ResolveServerHostname, v => v.ResolveServerHostnameCheckBox.Checked).DisposeWith(disposable);
                this.Bind(ViewModel, vm => vm.ServerTCPing, v => v.TCPingRadioBtn.Checked).DisposeWith(disposable);
                this.OneWayBind(ViewModel, vm => vm.ServerTCPing, v => v.ICMPingRadioBtn.Checked, b => !b).DisposeWith(disposable);
                this.Bind(ViewModel,
                        vm => vm.ProfileCount,
                        v => v.ProfileCountTextBox.Text,
                        v => v.ToString(),
                        v => uint.TryParse(v, out var c) ? (int) c : 0)
                    .DisposeWith(disposable);

                this.Bind(ViewModel,
                        vm => vm.DetectionTick,
                        v => v.DetectionTickTextBox.Text,
                        v => v.ToString(),
                        v => int.TryParse(v, out var c) && ServerHelper.DelayTestHelper.Range.InRange(c) ? c : -1)
                    .DisposeWith(disposable);

                this.Bind(ViewModel,
                        vm => vm.StartedPingInterval,
                        v => v.StartedPingIntervalTextBox.Text,
                        v => v.ToString(),
                        v => int.TryParse(v, out var i) && i >= -1 ? i : -1)
                    .DisposeWith(disposable);

                STUN_ServerComboBox.Items.AddRange(ReadSTUNs());

                object[] ReadSTUNs()
                {
                    try
                    {
                        return File.ReadLines("bin\\stun.txt").Cast<object>().ToArray();
                    }
                    catch (Exception e)
                    {
                        Logging.Warning($"Load stun.txt failed: {e.Message}");
                        return new object[0];
                    }
                }

                this.Bind(ViewModel, vm => vm.STUN_Server, v => v.STUN_ServerComboBox.Text);
                LanguageComboBox.Items.AddRange(i18N.GetTranslateList().Cast<object>().ToArray());
                this.Bind(ViewModel, vm => vm.Language, v => v.LanguageComboBox.SelectedItem, v => v, v => (string) v).DisposeWith(disposable);
            });

            #endregion

            #region Process Mode

            BindCheckBox(DNSHijackCheckBox, b => Global.Settings.Redirector.DNSHijack = b, Global.Settings.Redirector.DNSHijack);

            BindTextBox(DNSHijackHostTextBox, s => true, s => Global.Settings.Redirector.DNSHijackHost = s, Global.Settings.Redirector.DNSHijackHost);

            BindCheckBox(ICMPHijackCheckBox, b => Global.Settings.Redirector.ICMPHijack = b, Global.Settings.Redirector.ICMPHijack);

            BindTextBox(ICMPHijackHostTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.Redirector.ICMPHost = s,
                Global.Settings.Redirector.ICMPHost);

            BindCheckBox(RedirectorSSCheckBox, s => Global.Settings.Redirector.RedirectorSS = s, Global.Settings.Redirector.RedirectorSS);

            BindCheckBox(ChildProcessHandleCheckBox,
                s => Global.Settings.Redirector.ChildProcessHandle = s,
                Global.Settings.Redirector.ChildProcessHandle);

            BindListComboBox(ProcessProxyProtocolComboBox,
                s => Global.Settings.Redirector.ProxyProtocol = (PortType) Enum.Parse(typeof(PortType), s.ToString(), false),
                Enum.GetNames(typeof(PortType)),
                Global.Settings.Redirector.ProxyProtocol.ToString());

            #endregion

            #region TUN/TAP

            BindTextBox(TUNTAPAddressTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Address = s,
                Global.Settings.TUNTAP.Address);

            BindTextBox(TUNTAPNetmaskTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Netmask = s,
                Global.Settings.TUNTAP.Netmask);

            BindTextBox(TUNTAPGatewayTextBox,
                s => IPAddress.TryParse(s, out _),
                s => Global.Settings.TUNTAP.Gateway = s,
                Global.Settings.TUNTAP.Gateway);

            BindCheckBox(UseCustomDNSCheckBox, b => { Global.Settings.TUNTAP.UseCustomDNS = b; }, Global.Settings.TUNTAP.UseCustomDNS);

            BindTextBox(TUNTAPDNSTextBox,
                _ => true,
                s =>
                {
                    if (UseCustomDNSCheckBox.Checked)
                        Global.Settings.TUNTAP.HijackDNS = s;
                },
                Global.Settings.TUNTAP.HijackDNS);

            BindCheckBox(ProxyDNSCheckBox, b => Global.Settings.TUNTAP.ProxyDNS = b, Global.Settings.TUNTAP.ProxyDNS);

            #endregion

            #region V2Ray

            BindCheckBox(XrayConeCheckBox, b => Global.Settings.V2RayConfig.XrayCone = b, Global.Settings.V2RayConfig.XrayCone);

            BindCheckBox(TLSAllowInsecureCheckBox, b => Global.Settings.V2RayConfig.AllowInsecure = b, Global.Settings.V2RayConfig.AllowInsecure);
            BindCheckBox(UseMuxCheckBox, b => Global.Settings.V2RayConfig.UseMux = b, Global.Settings.V2RayConfig.UseMux);

            BindTextBox<int>(mtuTextBox, i => true, i => Global.Settings.V2RayConfig.KcpConfig.mtu = i, Global.Settings.V2RayConfig.KcpConfig.mtu);
            BindTextBox<int>(ttiTextBox, i => true, i => Global.Settings.V2RayConfig.KcpConfig.tti = i, Global.Settings.V2RayConfig.KcpConfig.tti);
            BindTextBox<int>(uplinkCapacityTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity = i,
                Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity);

            BindTextBox<int>(downlinkCapacityTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity = i,
                Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity);

            BindTextBox<int>(readBufferSizeTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.readBufferSize = i,
                Global.Settings.V2RayConfig.KcpConfig.readBufferSize);

            BindTextBox<int>(writeBufferSizeTextBox,
                i => true,
                i => Global.Settings.V2RayConfig.KcpConfig.writeBufferSize = i,
                Global.Settings.V2RayConfig.KcpConfig.writeBufferSize);

            BindCheckBox(congestionCheckBox,
                b => Global.Settings.V2RayConfig.KcpConfig.congestion = b,
                Global.Settings.V2RayConfig.KcpConfig.congestion);

            #endregion

            #region Others

            BindCheckBox(ExitWhenClosedCheckBox, b => Global.Settings.ExitWhenClosed = b, Global.Settings.ExitWhenClosed);

            BindCheckBox(StopWhenExitedCheckBox, b => Global.Settings.StopWhenExited = b, Global.Settings.StopWhenExited);

            BindCheckBox(StartWhenOpenedCheckBox, b => Global.Settings.StartWhenOpened = b, Global.Settings.StartWhenOpened);

            BindCheckBox(MinimizeWhenStartedCheckBox, b => Global.Settings.MinimizeWhenStarted = b, Global.Settings.MinimizeWhenStarted);

            BindCheckBox(RunAtStartupCheckBox, b => Global.Settings.RunAtStartup = b, Global.Settings.RunAtStartup);

            BindCheckBox(CheckUpdateWhenOpenedCheckBox, b => Global.Settings.CheckUpdateWhenOpened = b, Global.Settings.CheckUpdateWhenOpened);

            BindCheckBox(CheckBetaUpdateCheckBox, b => Global.Settings.CheckBetaUpdate = b, Global.Settings.CheckBetaUpdate);

            BindCheckBox(UpdateServersWhenOpenedCheckBox, b => Global.Settings.UpdateServersWhenOpened = b, Global.Settings.UpdateServersWhenOpened);

            #endregion

            #region AioDNS

            BindTextBox(AioDNSRulePathTextBox, _ => true, s => Global.Settings.AioDNS.RulePath = s, Global.Settings.AioDNS.RulePath);

            BindTextBox(ChinaDNSTextBox, _ => true, s => Global.Settings.AioDNS.ChinaDNS = s, Global.Settings.AioDNS.ChinaDNS);

            BindTextBox(OtherDNSTextBox, _ => true, s => Global.Settings.AioDNS.OtherDNS = s, Global.Settings.AioDNS.OtherDNS);

            #endregion
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            TUNTAPUseCustomDNSCheckBox_CheckedChanged(null, null);
        }

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object? sender, EventArgs? e)
        {
            if (UseCustomDNSCheckBox.Checked)
                TUNTAPDNSTextBox.Text = Global.Settings.TUNTAP.HijackDNS;
            else
                TUNTAPDNSTextBox.Text = "AioDNS";
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            Hide();
            new GlobalBypassIPForm().ShowDialog();
            Show();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Utils.Utils.ComponentIterator(this, component => Utils.Utils.ChangeControlForeColor(component, Color.Black));

            #region Check

            var checkNotPassControl = _checkActions.Where(pair => !pair.Value.Invoke(pair.Key.Text)).Select(pair => pair.Key).ToList();
            foreach (Control control in checkNotPassControl)
                Utils.Utils.ChangeControlForeColor(control, Color.Red);

            if (checkNotPassControl.Any())
                return;

            #endregion

            #region Save

            Global.Settings.Set(ViewModel);

            foreach (var pair in _saveActions)
                pair.Value.Invoke(pair.Key);

            #endregion

            Utils.Utils.RegisterNetchStartupItem();

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }

        #region BindUtils

        private void BindTextBox(TextBox control, Func<string, bool> check, Action<string> save, object value)
        {
            BindTextBox<string>(control, check, save, value);
        }

        private void BindTextBox<T>(TextBox control, Func<T, bool> check, Action<T> save, object value)
        {
            control.Text = value.ToString();
            _checkActions.Add(control,
                s =>
                {
                    try
                    {
                        return check.Invoke((T) Convert.ChangeType(s, typeof(T)));
                    }
                    catch
                    {
                        return false;
                    }
                });

            _saveActions.Add(control, c => save.Invoke((T) Convert.ChangeType(((TextBox) c).Text, typeof(T))));
        }

        private void BindCheckBox(CheckBox control, Action<bool> save, bool value)
        {
            control.Checked = value;
            _saveActions.Add(control, c => save.Invoke(((CheckBox) c).Checked));
        }

        private void BindRadioBox(RadioButton control, Action<bool> save, bool value)
        {
            control.Checked = value;
            _saveActions.Add(control, c => save.Invoke(((RadioButton) c).Checked));
        }

        private void BindListComboBox<T>(ComboBox comboBox, Action<T> save, IEnumerable<T> values, T value) where T : notnull
        {
            if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
                throw new ArgumentOutOfRangeException();

            var tagItems = values.Select(o => new TagItem<T>(o, o.ToString()!)).ToArray();
            comboBox.Items.AddRange(tagItems.Cast<object>().ToArray());

            comboBox.ValueMember = nameof(TagItem<T>.Value);
            comboBox.DisplayMember = nameof(TagItem<T>.Text);

            _saveActions.Add(comboBox, c => save.Invoke(((TagItem<T>) ((ComboBox) c).SelectedItem).Value));
            Load += (_, _) => { comboBox.SelectedItem = tagItems.SingleOrDefault(t => t.Value.Equals(value)); };
        }

        private void BindComboBox(ComboBox control, Func<string, bool> check, Action<string> save, string value, object[]? values = null)
        {
            if (values != null)
                control.Items.AddRange(values);

            _saveActions.Add(control, c => save.Invoke(((ComboBox) c).Text));
            _checkActions.Add(control, check.Invoke);

            Load += (_, _) => { control.Text = value; };
        }

        #endregion

        [NotNull]
        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (Setting?) value;
        }

        [NotNull]
        public Setting? ViewModel { get; set; }
    }
}