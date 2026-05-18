# Integrations

## LangChain

```python
from krnlai.integrations.langchain import KrnlAITool

tool = KrnlAITool(safety_level="strict")
```

## CrewAI

```python
from krnlai.integrations.crewai import KrnlAIAgent

agent = KrnlAIAgent(safety_level="strict")
```

## AutoGen

```python
from krnlai.integrations.autogen import KrnlAIAssistantAgent

agent = KrnlAIAssistantAgent(name="krnlai", safety_level="strict")
```

## FastAPI

```python
from fastapi import FastAPI
from krnlai.integrations.fastapi import KrnlAIMiddleware

app = FastAPI()
app.add_middleware(KrnlAIMiddleware)
```
