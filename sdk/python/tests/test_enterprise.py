from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from aikernel.enterprise.client import EnterpriseClient
from aikernel.enterprise.merge import CognitiveAgent
from aikernel.enterprise.streaming import EnterpriseStreamingClient


class TestEnterpriseClient:
    @pytest.mark.asyncio
    async def test_Health_WithMockEndpoint(self):
        client = EnterpriseClient(endpoint="http://test:5001", api_key="test-key")
        with patch.object(client._client, "get", new=AsyncMock()) as mock_get:
            mock_response = MagicMock()
            mock_response.status_code = 200
            mock_response.json.return_value = {"status": "ok"}
            mock_get.return_value = mock_response
            result = await client.health()
            assert result["status"] == "ok"


class TestEnterpriseStreamingClient:
    def test_Init_ShouldSetEndpoint(self):
        client = EnterpriseStreamingClient(endpoint="http://test:5001", api_key="key")
        assert client._endpoint == "http://test:5001"


class TestCognitiveAgent:
    def test_StandaloneMode_ShouldCreateRunner(self):
        agent = CognitiveAgent(mode="standalone")
        assert agent.is_standalone is True
        assert agent.mode == "standalone"

    def test_EnterpriseMode_ShouldCreateClient(self):
        agent = CognitiveAgent(mode="enterprise", endpoint="http://test:5001")
        assert agent.is_enterprise is True
        assert agent.mode == "enterprise"

    def test_AutoMode_WithoutEndpoint_ShouldBeStandalone(self):
        agent = CognitiveAgent(mode="auto")
        assert agent.is_standalone is True
        assert agent.is_enterprise is False

    @pytest.mark.asyncio
    async def test_Run_Standalone_ShouldReturnResult(self):
        agent = CognitiveAgent(mode="standalone")
        result = await agent.run("hello")
        assert result is not None
        assert hasattr(result, "output")

    @pytest.mark.asyncio
    async def test_Stream_Standalone_ShouldYieldEvents(self):
        agent = CognitiveAgent(mode="standalone")
        events = []
        async for event in agent.stream("hello"):
            events.append(event)
        assert len(events) > 0

    @pytest.mark.asyncio
    async def test_Close_ShouldNotRaise(self):
        agent = CognitiveAgent(mode="standalone")
        await agent.close()
