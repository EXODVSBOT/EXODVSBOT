using ExodvsBot.Domain.Dto;
using ExodvsBot.Domain.Enums;
using ExodvsBot.Runner;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ExodvsBot
{
    public partial class Form1 : Form
    {
        private System.ComponentModel.IContainer components = null;

        // Controles de entrada e configuração
        private TextBox txtApiKey;
        private TextBox txtApiSecret;
        private ComboBox cmbKlineInterval;
        private ComboBox cmbStoploss;
        private ComboBox cmbTakeProfit;
        private TextBox txtStoplossCustom;
        private TextBox txtTakeProfitCustom;
        private NumericUpDown numSellRSI;
        private NumericUpDown numBuyRSI;
        private ComboBox cmbRunInterval;
        private Panel statusIndicator;
        private ListBox lstLogs;
        private ListBox lstOcorrencias;
        private Timer updateTimer;

        // Botões de controle
        private Button btnIniciar;
        private Button btnParar;

        // Estado do bot
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;
        private string _statusBot = "Sleeping 😴";

        // Minimizar para segundo plano
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private async void InitializeComponent()
        {
            // Configura o comportamento ao fechar o formulário
            this.FormClosing += OnFormClosing;
            InitializeNotifyIcon(); // Adiciona o NotifyIcon

            // Configuração geral do formulário
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 800);
            this.BackColor = Color.FromArgb(54, 57, 63); // Fundo escuro (estilo Discord)
            this.ForeColor = Color.White;
            this.Text = "ExodvsBot";
            this.Font = new Font("Segoe UI", 9);
            string iconPath = Path.Combine("Images", "E.ico");
            this.Icon = new Icon(iconPath);

            // ******************************************************************
            // Layout principal utilizando TableLayoutPanel para responsividade
            // ******************************************************************
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // Linha para a arte ASCII
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // Linha para os demais controles
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            this.Controls.Add(mainLayout);

            // ******************************************************************
            // Arte ASCII no topo (linha 0, colunas 0-1)
            // ******************************************************************
            Label lblAsciiArt = new Label();
            lblAsciiArt.Text = @"
    ███████╗██╗  ██╗ ██████╗ ██████╗ ██╗   ██╗███████╗
    ██╔════╝╚██╗██╔╝██╔═══██╗██╔══██╗██║   ██║██╔════╝
    █████╗   ╚███╔╝ ██║   ██║██║  ██║██║   ██║███████╗
    ██╔══╝   ██╔██╗ ██║   ██║██║  ██║██║   ██║╚════██║
    ███████╗██╔╝ ██╗╚██████╔╝██████╔╝╚██████╔╝███████║
    ╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝  ╚═════╝ ╚══════╝";
            lblAsciiArt.AutoSize = false;
            lblAsciiArt.Dock = DockStyle.Fill;
            lblAsciiArt.ForeColor = Color.FromArgb(114, 137, 218); // Azul do Discord
            lblAsciiArt.Font = new Font("Consolas", 10, FontStyle.Bold);
            lblAsciiArt.TextAlign = ContentAlignment.MiddleCenter;
            mainLayout.Controls.Add(lblAsciiArt, 0, 0);
            mainLayout.SetColumnSpan(lblAsciiArt, 2);

            // ******************************************************************
            // Painel de Configuração (linha 1, coluna 0)
            // ******************************************************************
            Panel configPanel = new Panel();
            configPanel.Dock = DockStyle.Fill;
            configPanel.Padding = new Padding(20);

            // Layout interno para os controles de configuração
            TableLayoutPanel configLayout = new TableLayoutPanel();
            configLayout.Dock = DockStyle.Fill;
            configLayout.ColumnCount = 3;
            configLayout.RowCount = 9;
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            // Cada linha terá altura fixa para manter o espaçamento
            for (int i = 0; i < 8; i++)
                configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Linha para botões e status

            // API Key
            Label lblApiKey = new Label();
            lblApiKey.Text = "API Key:";
            lblApiKey.ForeColor = Color.White;
            lblApiKey.TextAlign = ContentAlignment.MiddleLeft;
            lblApiKey.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblApiKey, 0, 0);

            txtApiKey = new TextBox();
            txtApiKey.Dock = DockStyle.Fill;
            txtApiKey.BackColor = Color.FromArgb(44, 47, 51);
            txtApiKey.ForeColor = Color.White;
            txtApiKey.BorderStyle = BorderStyle.FixedSingle;
            configLayout.Controls.Add(txtApiKey, 1, 0);
            // Coluna 2 vazia para este campo

            // API Secret
            Label lblApiSecret = new Label();
            lblApiSecret.Text = "API Secret:";
            lblApiSecret.ForeColor = Color.White;
            lblApiSecret.TextAlign = ContentAlignment.MiddleLeft;
            lblApiSecret.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblApiSecret, 0, 1);

            txtApiSecret = new TextBox();
            txtApiSecret.Dock = DockStyle.Fill;
            txtApiSecret.BackColor = Color.FromArgb(44, 47, 51);
            txtApiSecret.ForeColor = Color.White;
            txtApiSecret.BorderStyle = BorderStyle.FixedSingle;
            configLayout.Controls.Add(txtApiSecret, 1, 1);

            // Kline Interval
            Label lblKlineInterval = new Label();
            lblKlineInterval.Text = "Kline Interval:";
            lblKlineInterval.ForeColor = Color.White;
            lblKlineInterval.TextAlign = ContentAlignment.MiddleLeft;
            lblKlineInterval.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblKlineInterval, 0, 2);

            cmbKlineInterval = new ComboBox();
            cmbKlineInterval.Dock = DockStyle.Fill;
            cmbKlineInterval.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbKlineInterval.BackColor = Color.FromArgb(44, 47, 51);
            cmbKlineInterval.ForeColor = Color.White;
            cmbKlineInterval.FlatStyle = FlatStyle.Flat;


            foreach (KlineIntervalEnum interval in Enum.GetValues(typeof(KlineIntervalEnum)))
                cmbKlineInterval.Items.Add(interval);

            cmbKlineInterval.SelectedIndex = 9;
            configLayout.Controls.Add(cmbKlineInterval, 1, 2);

            // Stoploss
            Label lblStoploss = new Label();
            lblStoploss.Text = "Stoploss (%):";
            lblStoploss.ForeColor = Color.White;
            lblStoploss.TextAlign = ContentAlignment.MiddleLeft;
            lblStoploss.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblStoploss, 0, 3);

            cmbStoploss = new ComboBox();
            cmbStoploss.Dock = DockStyle.Fill;
            cmbStoploss.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStoploss.BackColor = Color.FromArgb(44, 47, 51);
            cmbStoploss.ForeColor = Color.White;
            cmbStoploss.FlatStyle = FlatStyle.Flat;
            cmbStoploss.Items.AddRange(new string[] { "1", "2", "5", "10", "Custom" });
            cmbStoploss.SelectedIndex = 3;
            cmbStoploss.SelectedIndexChanged += (sender, e) =>
            {
                if (cmbStoploss.SelectedItem.ToString() == "Custom")
                {
                    txtStoplossCustom.Visible = true;
                    txtStoplossCustom.Focus();
                }
                else
                {
                    txtStoplossCustom.Visible = false;
                }
            };
            configLayout.Controls.Add(cmbStoploss, 1, 3);

            txtStoplossCustom = new TextBox();
            txtStoplossCustom.Dock = DockStyle.Fill;
            txtStoplossCustom.BackColor = Color.FromArgb(44, 47, 51);
            txtStoplossCustom.ForeColor = Color.White;
            txtStoplossCustom.BorderStyle = BorderStyle.FixedSingle;
            txtStoplossCustom.Visible = false;
            txtStoplossCustom.KeyPress += (sender, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                {
                    e.Handled = true;
                }
                if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
                {
                    e.Handled = true;
                }
            };
            configLayout.Controls.Add(txtStoplossCustom, 2, 3);

            // TakeProfit
            Label lblTakeProfit = new Label();
            lblTakeProfit.Text = "TakeProfit (%):";
            lblTakeProfit.ForeColor = Color.White;
            lblTakeProfit.TextAlign = ContentAlignment.MiddleLeft;
            lblTakeProfit.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblTakeProfit, 0, 4);

            cmbTakeProfit = new ComboBox();
            cmbTakeProfit.Dock = DockStyle.Fill;
            cmbTakeProfit.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTakeProfit.BackColor = Color.FromArgb(44, 47, 51);
            cmbTakeProfit.ForeColor = Color.White;
            cmbTakeProfit.FlatStyle = FlatStyle.Flat;
            cmbTakeProfit.Items.AddRange(new string[] { "1", "2", "5", "10", "Custom" });
            cmbTakeProfit.SelectedIndex = 0;
            cmbTakeProfit.SelectedIndexChanged += (sender, e) =>
            {
                if (cmbTakeProfit.SelectedItem.ToString() == "Custom")
                {
                    txtTakeProfitCustom.Visible = true;
                    txtTakeProfitCustom.Focus();
                }
                else
                {
                    txtTakeProfitCustom.Visible = false;
                }
            };
            configLayout.Controls.Add(cmbTakeProfit, 1, 4);

            txtTakeProfitCustom = new TextBox();
            txtTakeProfitCustom.Dock = DockStyle.Fill;
            txtTakeProfitCustom.BackColor = Color.FromArgb(44, 47, 51);
            txtTakeProfitCustom.ForeColor = Color.White;
            txtTakeProfitCustom.BorderStyle = BorderStyle.FixedSingle;
            txtTakeProfitCustom.Visible = false;
            txtTakeProfitCustom.KeyPress += (sender, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                {
                    e.Handled = true;
                }
                if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
                {
                    e.Handled = true;
                }
            };
            configLayout.Controls.Add(txtTakeProfitCustom, 2, 4);

            // Buy RSI
            Label lblBuyRSI = new Label();
            lblBuyRSI.Text = "Buy RSI:";
            lblBuyRSI.ForeColor = Color.White;
            lblBuyRSI.TextAlign = ContentAlignment.MiddleLeft;
            lblBuyRSI.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblBuyRSI, 0, 5);

            numBuyRSI = new NumericUpDown();
            numBuyRSI.Dock = DockStyle.Fill;
            numBuyRSI.BackColor = Color.FromArgb(44, 47, 51);
            numBuyRSI.ForeColor = Color.White;
            numBuyRSI.BorderStyle = BorderStyle.FixedSingle;
            numBuyRSI.Minimum = 0;
            numBuyRSI.Maximum = 100;
            numBuyRSI.Value = 30;
            configLayout.Controls.Add(numBuyRSI, 1, 5);

            // Sell RSI
            Label lblSellRSI = new Label();
            lblSellRSI.Text = "Sell RSI:";
            lblSellRSI.ForeColor = Color.White;
            lblSellRSI.TextAlign = ContentAlignment.MiddleLeft;
            lblSellRSI.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblSellRSI, 0, 6);

            numSellRSI = new NumericUpDown();
            numSellRSI.Dock = DockStyle.Fill;
            numSellRSI.BackColor = Color.FromArgb(44, 47, 51);
            numSellRSI.ForeColor = Color.White;
            numSellRSI.BorderStyle = BorderStyle.FixedSingle;
            numSellRSI.Minimum = 0;
            numSellRSI.Maximum = 100;
            numSellRSI.Value = 70;
            configLayout.Controls.Add(numSellRSI, 1, 6);

            // Run Interval
            Label lblRunInterval = new Label();
            lblRunInterval.Text = "Run Interval:";
            lblRunInterval.ForeColor = Color.White;
            lblRunInterval.TextAlign = ContentAlignment.MiddleLeft;
            lblRunInterval.Dock = DockStyle.Fill;
            configLayout.Controls.Add(lblRunInterval, 0, 7);

            cmbRunInterval = new ComboBox();
            cmbRunInterval.Dock = DockStyle.Fill;
            cmbRunInterval.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRunInterval.BackColor = Color.FromArgb(44, 47, 51);
            cmbRunInterval.ForeColor = Color.White;
            cmbRunInterval.FlatStyle = FlatStyle.Flat;
            foreach (RunIntervalEnum interval in Enum.GetValues(typeof(RunIntervalEnum)))
                cmbRunInterval.Items.Add(interval);
            cmbRunInterval.SelectedIndex = 1;
            configLayout.Controls.Add(cmbRunInterval, 1, 7);

            // Botões de controle e indicador de status (última linha)
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.AutoSize = true;

            btnIniciar = new Button();
            btnIniciar.Text = "▶ Run Bot";
            btnIniciar.Size = new Size(150, 40);
            btnIniciar.BackColor = Color.FromArgb(114, 137, 218);
            btnIniciar.ForeColor = Color.White;
            btnIniciar.FlatStyle = FlatStyle.Flat;
            btnIniciar.FlatAppearance.BorderSize = 0;
            btnIniciar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnIniciar.Click += new EventHandler(btnStartBot_Click);
            buttonPanel.Controls.Add(btnIniciar);

            btnParar = new Button();
            btnParar.Text = "⏹ Stop Bot";
            btnParar.Size = new Size(150, 40);
            btnParar.BackColor = Color.FromArgb(237, 66, 69);
            btnParar.ForeColor = Color.White;
            btnParar.FlatStyle = FlatStyle.Flat;
            btnParar.FlatAppearance.BorderSize = 0;
            btnParar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnParar.Click += new EventHandler(BtnStopBot_Click);
            btnParar.Visible = false;
            buttonPanel.Controls.Add(btnParar);

            // Indicador de status
            Panel statusIndicatorPanel = new Panel();
            statusIndicatorPanel.Size = new Size(20, 20);
            statusIndicatorPanel.BackColor = Color.Gray;
            statusIndicatorPanel.BorderStyle = BorderStyle.FixedSingle;
            buttonPanel.Controls.Add(statusIndicatorPanel);
            this.statusIndicator = statusIndicatorPanel;

            Label lblStatus = new Label();
            lblStatus.Text = _statusBot;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.White;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            buttonPanel.Controls.Add(lblStatus);

            // Atualiza a label de status quando os botões são clicados
            btnIniciar.Click += (sender, e) => lblStatus.Text = _statusBot;
            btnParar.Click += (sender, e) => lblStatus.Text = _statusBot;

            configLayout.Controls.Add(buttonPanel, 0, 8);
            configLayout.SetColumnSpan(buttonPanel, 3);

            configPanel.Controls.Add(configLayout);
            mainLayout.Controls.Add(configPanel, 0, 1);

            // ******************************************************************
            // Painel de Logs e Trades (linha 1, coluna 1)
            // ******************************************************************
            Panel logsPanel = new Panel();
            logsPanel.Dock = DockStyle.Fill;
            logsPanel.Padding = new Padding(20);

            TableLayoutPanel logsLayout = new TableLayoutPanel();
            logsLayout.Dock = DockStyle.Fill;
            logsLayout.ColumnCount = 1;
            logsLayout.RowCount = 4;
            logsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Label "Logs"
            logsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));     // ListBox de logs
            logsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Label "Trades"
            logsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));     // ListBox de trades

            Label lblLogs = new Label();
            lblLogs.Text = "Logs:";
            lblLogs.ForeColor = Color.White;
            lblLogs.Dock = DockStyle.Fill;
            logsLayout.Controls.Add(lblLogs, 0, 0);

            lstLogs = new ListBox();
            lstLogs.Dock = DockStyle.Fill;
            lstLogs.BackColor = Color.FromArgb(44, 47, 51);
            lstLogs.ForeColor = Color.White;
            lstLogs.BorderStyle = BorderStyle.FixedSingle;
            logsLayout.Controls.Add(lstLogs, 0, 1);

            Label lblOcorrencias = new Label();
            lblOcorrencias.Text = "Trades:";
            lblOcorrencias.ForeColor = Color.White;
            lblOcorrencias.Dock = DockStyle.Fill;
            logsLayout.Controls.Add(lblOcorrencias, 0, 2);

            lstOcorrencias = new ListBox();
            lstOcorrencias.Dock = DockStyle.Fill;
            lstOcorrencias.BackColor = Color.FromArgb(44, 47, 51);
            lstOcorrencias.ForeColor = Color.White;
            lstOcorrencias.BorderStyle = BorderStyle.FixedSingle;
            logsLayout.Controls.Add(lstOcorrencias, 0, 3);

            logsPanel.Controls.Add(logsLayout);
            mainLayout.Controls.Add(logsPanel, 1, 1);

            // Configuração do Timer de atualização dos logs
            SetupUpdateTimer();
        }
        #endregion

        private async void btnStartBot_Click(object sender, EventArgs e)
        {
            // Se o cancellationTokenSource já estiver ativo, evita iniciar outro bot
            if (cancellationTokenSource != null)
                return;

            // Alterna a visibilidade dos botões: esconde o botão "Run Bot" e mostra o "Stop Bot"
            btnIniciar.Visible = false;
            btnParar.Visible = true;

            // Atualiza a aparência para "rodando"
            statusIndicator.BackColor = Color.Green;
            cancellationTokenSource = new CancellationTokenSource();
            _statusBot = "Running 🚀";

            // Verifica se o valor de Stoploss é customizado
            int stoploss;
            if (cmbStoploss.SelectedItem.ToString() == "Custom" && !string.IsNullOrEmpty(txtStoplossCustom.Text))
            {
                stoploss = int.Parse(txtStoplossCustom.Text);
            }
            else
            {
                stoploss = int.Parse(cmbStoploss.SelectedItem.ToString());
            }

            // Verifica se o valor de TakeProfit é customizado
            int takeProfit;
            if (cmbTakeProfit.SelectedItem.ToString() == "Custom" && !string.IsNullOrEmpty(txtTakeProfitCustom.Text))
            {
                takeProfit = int.Parse(txtTakeProfitCustom.Text);
            }
            else
            {
                takeProfit = int.Parse(cmbTakeProfit.SelectedItem.ToString());
            }

            var startSettingsDto = new StartSettingsDto()
            {
                txtApiKey = txtApiKey.Text,
                txtApiSecret = txtApiSecret.Text,
                cmbKlineInterval = (KlineIntervalEnum)cmbKlineInterval.SelectedItem,
                cmbRunInterval = (RunIntervalEnum)cmbRunInterval.SelectedItem,
                cmbStoploss = stoploss,
                cmbTakeProfit = takeProfit,
                numBuyRSI = (int)numBuyRSI.Value,
                numSellRSI = (int)numSellRSI.Value,
            };

            try
            {
                await Task.Run(() => Runner.Runner.RunAsyncn(
                    cancellationTokenSource.Token,
                    startSettingsDto,
                    ex => this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show($"Erro: {ex.Message}");
                        isRunning = false;
                        statusIndicator.BackColor = Color.Red;
                    }))
                ));
            }
            catch (OperationCanceledException)
            {
                // Bot foi parado
            }
            finally
            {
                isRunning = false;
                statusIndicator.BackColor = Color.Red;
                // **Importante:** reseta o cancellationTokenSource para permitir nova execução
                cancellationTokenSource = null;

                // Quando o processamento terminar, alterna os botões:
                btnIniciar.Visible = true;
                btnParar.Visible = false;
            }
        }

        private void BtnStopBot_Click(object sender, EventArgs e)
        {
            // Cancela a execução do bot
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            isRunning = false;
            statusIndicator.BackColor = Color.Red;
            _statusBot = "Stopped 🚫";

            // Limpa os logs e ocorrências
            Runner.Runner.Logs.Clear(); // Limpa a lista de logs
            Runner.Runner.Ocorrencias.Clear(); // Limpa a lista de ocorrências

            // Limpa as listas no formulário
            lstLogs.Items.Clear();
            lstOcorrencias.Items.Clear();

            // Alterna a visibilidade: mostra o botão "Run Bot" e esconde o "Stop Bot"
            btnIniciar.Visible = true;
            btnParar.Visible = false;
        }

        private void SetupUpdateTimer()
        {
            updateTimer = new Timer();
            updateTimer.Interval = 1000; // Atualiza a cada 1 segundo
            updateTimer.Tick += UpdateLogsDisplay;
            updateTimer.Start();
        }

        private void UpdateLogsDisplay(object sender, EventArgs e)
        {
            // Atualiza os logs: limpa e adiciona os itens atuais
            lstLogs.BeginUpdate();
            lstLogs.Items.Clear();
            foreach (var log in Runner.Runner.Logs.ToList())
            {
                // Adiciona o log no início da lista (índice 0)
                lstLogs.Items.Insert(0, log);
            }
            lstLogs.EndUpdate();

            // Atualiza as ocorrências: limpa e adiciona os itens atuais
            lstOcorrencias.BeginUpdate();
            lstOcorrencias.Items.Clear();
            foreach (var ocorrencia in Runner.Runner.Ocorrencias.ToList())
            {
                // Adiciona a ocorrência no início da lista (índice 0)
                lstOcorrencias.Items.Insert(0, FormatOcorrencia(ocorrencia));
            }
            lstOcorrencias.EndUpdate();
        }

        private string FormatOcorrencia(OcorrenciaDto ocorrencia)
        {
            return $"[{ocorrencia.Data:dd/MM/yyyy HH:mm:ss}] - {ocorrencia.SaldoUsdt.ToString("0.00")} - {ocorrencia.Decisao} - {ocorrencia.PrecoBitcoin.ToString("0.00")}";
        }

        private void InitializeNotifyIcon()
        {
            // Cria o NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon(Path.Combine("Images", "E.ico")); // Usa o mesmo ícone do formulário
            notifyIcon.Text = "ExodvsBot";
            notifyIcon.Visible = true;

            // Cria o menu de contexto
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Abrir", null, OnOpenClicked);
            contextMenu.Items.Add("Fechar", null, OnCloseClicked);

            // Associa o menu de contexto ao NotifyIcon
            notifyIcon.ContextMenuStrip = contextMenu;

            // Configura o evento de clique duplo para restaurar o formulário
            notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
        }

        private void OnOpenClicked(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                notifyIcon.Visible = true;
            }
        }
    }
}
