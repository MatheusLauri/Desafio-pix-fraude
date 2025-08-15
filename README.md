# 🏦 FinSync – Detecção de Fraudes em Pix

O **FinSync** é um projeto pessoal focado em aprender e aplicar conceitos de backend para fintechs, com ênfase em processamento em tempo real, mensageria e detecção de fraudes em transações Pix.

## 🚀 Tecnologias Utilizadas

- **.NET 8 (ASP.NET Core)** – API backend
- **SQL Server** – Banco de dados relacional
- **RabbitMQ** – Mensageria para processamento assíncrono
- **Docker** – Ambiente de containers (não incluído no repositório, mas utilizado localmente)

## 📌 Funcionalidades

- Cadastro e envio de transações Pix
- Publicação de mensagens em fila (RabbitMQ)
- Consumo de mensagens em background service
- Validação e sinalização de possíveis fraudes
- Logs e rastreabilidade de eventos

## ⚙️ Como Rodar Localmente

### Pré-requisitos

- .NET 8 SDK
- SQL Server (local ou em container)
- RabbitMQ (local ou em container)

### Configuração do Banco de Dados

Crie um banco de dados no SQL Server:

```sql
CREATE DATABASE FinSyncDb;
```

Ajuste a connection string no `appsettings.json` do projeto:

```json
"ConnectionStrings": {
  "SqlServer": "Server=localhost,1433;Database=FinSyncDb;User Id=sa;Password=YourPassword;"
}
```

### Configuração do RabbitMQ

No `appsettings.json`, defina o host do RabbitMQ (padrão: localhost):

```json
"RabbitMQ": {
  "HostName": "localhost",
  "UserName": "guest",
  "Password": "guest"
}
```

### Executando a API

No diretório raiz do projeto, rode:

```sh
dotnet build
dotnet run
```

A API estará disponível em:

- [https://localhost:5001](https://localhost:5001)
- [http://localhost:5000](http://localhost:5000)

### Testando com Postman

Envie uma requisição **POST** para:

```
POST /api/transactions
```

Exemplo de body:

```json
{
  "SenderId": "e7b8f6c1-1234-4d5a-9f2b-0a1b2c3d4e5f",
  "ReceiverId": "a1b2c3d4-5678-4e9f-8a7b-0c1d2e3f4a5b",
  "PixKey": "teste@pix.com",
  "Amount": 1500.75
}
```

Se a transação for considerada suspeita, o consumidor registrará a fraude no console.

## 📷 Demonstração

Exemplo de mensagem detectada como fraude pelo consumer:

*(Adicione aqui prints ou exemplos de saída do console)*

## 🛠️ Próximos Passos

- Implementar testes automatizados
- Criar API para histórico de fraudes
- Expandir regras de validação

---

🔗 **Autor: Matheus Lauri**