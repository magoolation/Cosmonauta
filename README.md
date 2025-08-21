# Cosmonauta - Azure CosmosDB Explorer

Um explorador de dados interativo para Azure CosmosDB usando os SDKs oficiais e uma interface elegante com Spectre.Console.

## Funcionalidades

- ✅ **Seleção de Subscription**: Escolha qual subscription Azure usar
- ✅ **Navegação Hierárquica**: Resource Groups → Cosmos Accounts → Databases → Collections → Documents
- ✅ **Conexão Direta**: Conecte usando endpoint e chave
- ✅ **Queries SQL**: Execute queries SQL API diretamente
- ✅ **Geração de cURL**: Gere exemplos de comandos cURL para integração
- ✅ **Interface Elegante**: UI colorida e interativa com Spectre.Console

## Pré-requisitos

- .NET 8.0 ou superior
- Azure CLI instalado e configurado (`az login`)
- Permissões adequadas nas subscriptions Azure

## Instalação

```bash
# Clone o repositório
git clone <seu-repositorio>
cd Cosmonauta

# Restaurar pacotes
dotnet restore

# Compilar
dotnet build
```

## Uso

```bash
# Executar o programa
dotnet run
```

### Menu Principal

1. **Selecionar/Alterar Subscription**: Lista todas as subscriptions disponíveis e permite selecionar uma
2. **Explorar por Resource Group**: Navega pelos Resource Groups da subscription selecionada
3. **Listar todas as Contas CosmosDB**: Mostra todas as contas CosmosDB da subscription
4. **Conectar diretamente**: Conecta usando endpoint e chave primária

### Fluxo de Navegação

```
Subscription → Resource Group → Cosmos Account → Database → Collection → Documents/Queries
```

### Conexão Direta

Para conexão direta, você precisa:
- **Endpoint**: `https://sua-conta.documents.azure.com:443/`
- **Chave Primária**: Disponível no portal Azure

## Troubleshooting

### Problema: Não lista subscriptions
**Solução**: 
- Verifique se está logado: `az account show`
- Se não estiver: `az login`

### Problema: Não encontra Resource Groups
**Solução**:
- Selecione uma subscription primeiro no menu principal
- Verifique permissões na subscription

### Problema: Erro ao conectar com endpoint/chave
**Solução**:
- Verifique o formato do endpoint (deve incluir https:// e porta :443)
- Confirme que a chave primária está correta
- Verifique se o firewall do CosmosDB permite seu IP

## Estrutura do Projeto

```
Cosmonauta/
├── Models/          # Modelos de dados
├── Services/        # Serviços de Azure e CosmosDB
├── UI/              # Interface de usuário com Spectre.Console
└── Program.cs       # Ponto de entrada
```

## Tecnologias Utilizadas

- **Azure.Identity**: Autenticação com Azure
- **Azure.ResourceManager**: Gerenciamento de recursos Azure
- **Azure.ResourceManager.CosmosDB**: Gerenciamento específico do CosmosDB
- **Microsoft.Azure.Cosmos**: SDK de dados do CosmosDB
- **Spectre.Console**: Interface de linha de comando elegante
- **Newtonsoft.Json**: Serialização JSON
- **TextCopy**: Cópia para área de transferência

## Licença

MIT