using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Conectar
{
    public class Person
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string ci { get; set; }
        public string phone { get; set; }
    }

    public class GraphQLResponse<T>
    {
        public T data { get; set; }
    }

    public class QueryData
    {
        public List<Person> people { get; set; }
    }

    public partial class Form1 : Form
    {
        private readonly HttpClient client = new HttpClient();
        private readonly string apiUrl = "http://127.0.0.1:8000/graphql";

        public Form1()
        {
            InitializeComponent();
        }

        private async Task<string> SendGraphQLRequest(string query, object variables = null)
        {
            if (string.IsNullOrWhiteSpace(txtToken.Text))
            {
                MessageBox.Show("Por favor, pega primero tu Token JWT generado en Insomnia.");
                return null;
            }

            // Armamos el cuerpo de la petición como le gusta a GraphQL
            var requestBody = new
            {
                query = query,
                variables = variables
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Inyectamos tu Token como salvoconducto de seguridad
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", txtToken.Text);

            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Error del servidor: " + response.StatusCode + "\n" + responseString);
                    return null;
                }

                return responseString;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión: Verifica que tu API de Laravel esté encendida. " + ex.Message);
                return null;
            }
        }

        private async void btnListar_Click(object sender, EventArgs e)
        {
            string query = @"
            query {
                people {
                    id first_name last_name ci phone
                }
            }";

            string responseJson = await SendGraphQLRequest(query);

            if (responseJson != null)
            {
                var result = JsonSerializer.Deserialize<GraphQLResponse<QueryData>>(responseJson);
                if (result?.data?.people != null)
                {
                    dgvPersonas.DataSource = result.data.people;
                }
            }
        }

        // --- BOTÓN: GUARDAR (Crea si no hay ID, Actualiza si ya hay ID) ---
        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            string query;
            object variables;

            if (string.IsNullOrEmpty(txtId.Text))
            {
                // Modo CREATE
                query = @"
                mutation($fn: String!, $ln: String!, $ci: String!, $phone: String) {
                    createPerson(first_name: $fn, last_name: $ln, ci: $ci, phone: $phone) {
                        id
                    }
                }";

                variables = new
                {
                    fn = txtFirstName.Text,
                    ln = txtLastName.Text,
                    ci = txtCI.Text,
                    phone = txtPhone.Text
                };
            }
            else
            {
                // Modo UPDATE
                query = @"
                mutation($id: ID!, $fn: String, $ln: String, $ci: String, $phone: String) {
                    updatePerson(id: $id, first_name: $fn, last_name: $ln, ci: $ci, phone: $phone) {
                        id
                    }
                }";

                variables = new
                {
                    id = txtId.Text,
                    fn = txtFirstName.Text,
                    ln = txtLastName.Text,
                    ci = txtCI.Text,
                    phone = txtPhone.Text
                };
            }

            var response = await SendGraphQLRequest(query, variables);
            if (response != null)
            {
                MessageBox.Show("¡Guardado exitosamente!");
                btnListar_Click(null, null); // Recargamos la tabla
                btnLimpiar_Click(null, null); // Limpiamos campos
            }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtId.Text))
            {
                MessageBox.Show("Por favor, haz clic en una persona de la tabla primero.");
                return;
            }

            string query = @"
            mutation($id: ID!) {
                deletePerson(id: $id) {
                    id
                }
            }";

            var variables = new { id = txtId.Text };

            var response = await SendGraphQLRequest(query, variables);
            if (response != null)
            {
                MessageBox.Show("¡Eliminado exitosamente!");
                btnListar_Click(null, null);
                btnLimpiar_Click(null, null);
            }
        }
        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtId.Clear();
            txtFirstName.Clear();
            txtLastName.Clear();
            txtCI.Clear();
            txtPhone.Clear();
        }

        private void dgvPersonas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Verificamos que se haya hecho clic en una fila válida y no en los encabezados
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvPersonas.Rows[e.RowIndex];
                txtId.Text = row.Cells["id"].Value?.ToString();
                txtFirstName.Text = row.Cells["first_name"].Value?.ToString();
                txtLastName.Text = row.Cells["last_name"].Value?.ToString();
                txtCI.Text = row.Cells["ci"].Value?.ToString();
                txtPhone.Text = row.Cells["phone"].Value?.ToString();
            }
        }
    }
}