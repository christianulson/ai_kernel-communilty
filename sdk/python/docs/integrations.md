# Integrations

## LangChain

```python
from aikernel.integrations.langchain import AikernelTool

tool = AikernelTool(safety_level="strict")
```

## CrewAI

```python
from aikernel.integrations.crewai import AikernelAgent

agent = AikernelAgent(safety_level="strict")
```

## AutoGen

```python
from aikernel.integrations.autogen import AikernelAssistantAgent

agent = AikernelAssistantAgent(name="aikernel", safety_level="strict")
```

## FastAPI

```python
from fastapi import FastAPI
from aikernel.integrations.fastapi import AikernelMiddleware

app = FastAPI()
app.add_middleware(AikernelMiddleware)
```
