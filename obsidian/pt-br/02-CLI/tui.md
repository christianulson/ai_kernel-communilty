# TUI (Interface de Terminal)

A CLI inclui uma interface de terminal ativada com `krnlai chat --local`.

## Recursos

- Layout de painel dividido com historico de chat e status
- Gerenciamento de sessao em tempo real
- Alternancia de provedor e modelo
- Busca de memoria integrada
- Exibicao de status de verificacao de seguranca

## Atalhos de Teclado

| Tecla | Acao |
|-----|--------|
| `Ctrl+C` | Sair |
| `Ctrl+L` | Limpar tela |
| `Ctrl+S` | Salvar sessao |
| `Tab` | Focar proximo painel |
| `Cima/Baixo` | Navegar historico |
| `Ctrl+R` | Pesquisar memoria |
| `Esc` | Cancelar / Fechar painel |

## Painel de Status

O painel de status exibe:
- Provedor e modelo conectados
- Estatisticas de uso de memoria
- Camadas de seguranca ativas
- ID da sessao atual
- Indicadores de erro e aviso
