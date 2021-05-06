namespace Cambios
{
    using Modelos;
    using Servicos;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        #region Atributos

        private List<Rate> Rates;

        private NetworkService networkService;

        private ApiService apiService;

        private DialogService dialogService;

        private DataService dataService;

        #endregion

        public Form1()
        {
            InitializeComponent();
            networkService = new NetworkService();
            apiService = new ApiService();
            dialogService = new DialogService();
            dataService = new DataService();
            LoadRates();
        }

        private async void LoadRates()
        {
            bool load;

            LabelResultado.Text = "A atualizar taxas...";

            var connection = networkService.CheckConnection();

            if (!connection.IsSuccess)
            {
                LoadLocalRates();
                load = false;
            }
            else
            {
                await LoadApiRates();
                load = true;
            }

            if (Rates.Count == 0)
            {
                LabelResultado.Text = "Não há ligação á Internet" + Environment.NewLine +
                    "e não foram previamente carregadas as taxas." + Environment.NewLine +
                    "Tente mais tarde!";

                LabelStatus.Text = "Primeira inicialização deverá ter ligação á Internet";

                return;
            }

            ComboBoxOrigem.DataSource = Rates;
            ComboBoxOrigem.DisplayMember = "Name";

            //Corrige bug da microsoft
            ComboBoxDestino.BindingContext = new BindingContext();

            ComboBoxDestino.DataSource = Rates;

            ComboBoxDestino.DisplayMember = "Name";

            LabelResultado.Text = "Taxas atualizadas...";

            if (load)
            {
                LabelStatus.Text = string.Format("Taxas carregadas da Internet em {0:F}", DateTime.Now);
            }
            else
            {
                LabelStatus.Text = string.Format("Taxas carregadas da Base de Dados");
            }

            ProgressBar1.Value = 100;

            ButtonConverter.Enabled = true;
            ButtonTroca.Enabled = true;
        }

        private void LoadLocalRates()
        {
            Rates = dataService.GetData();
        }

        private async Task LoadApiRates()
        {
            ProgressBar1.Value = 0;

            var response = await apiService.GetRates("http://cambios.somee.com", "/api/rates");

            Rates = (List<Rate>)response.Result;

            dataService.DeleteData();

            dataService.SaveData(Rates);
        }

        private void ButtonConverter_Click(object sender, EventArgs e)
        {
            Converter();
        }

        private void Converter()
        {
            if (string.IsNullOrEmpty(TextBoxValor.Text))
            {
                dialogService.ShowMessage("Erro", "Insira um valor a converter");
                return;
            }

            decimal valor;
            if (!decimal.TryParse(TextBoxValor.Text, out valor))
            {
                dialogService.ShowMessage("Erro de Conversão", "Valor terá que ser numérico");
                return;
            }

            if (ComboBoxOrigem.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda a converter");
                return;
            }

            if (ComboBoxDestino.SelectedItem == null)
            {
                dialogService.ShowMessage("Erro", "Tem que escolher uma moeda de destino para converter");
                return;
            }

            var taxaOrigem = (Rate)ComboBoxOrigem.SelectedItem;

            var taxaDestino = (Rate)ComboBoxDestino.SelectedItem;

            var ValorConvertido = valor / (decimal)taxaOrigem.TaxRate * (decimal)taxaDestino.TaxRate;

            LabelResultado.Text = string.Format("{0} {1:C2} = {2} {3:C}",
                taxaOrigem.Code, valor, taxaDestino.Code, ValorConvertido);
        }

        private void ButtonTroca_Click(object sender, EventArgs e)
        {
            Troca();
        }

        private void Troca()
        {
            var aux = ComboBoxOrigem.SelectedItem;
            ComboBoxOrigem.SelectedItem = ComboBoxDestino.SelectedItem;
            ComboBoxDestino.SelectedItem = aux;
            Converter();
        }


    }
}
