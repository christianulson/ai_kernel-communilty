# Integrations

## LangChain

```python
from krnlai.integrations.langchain import AikernelTool

tool = AikernelTool(safety_level="strict")
```

## CrewAI

```python
from krnlai.integrations.crewai import AikernelAgent

agent = AikernelAgent(safety_level="strict")
```

## AutoGen

```python
from krnlai.integrations.autogen import AikernelAssistantAgent

agent = AikernelAssistantAgent(name="krnlai", safety_level="strict")
```

## FastAPI

```python
from fastapi import FastAPI
from krnlai.integrations.fastapi import AikernelMiddleware

app = FastAPI()
app.add_middleware(AikernelMiddleware)
```
