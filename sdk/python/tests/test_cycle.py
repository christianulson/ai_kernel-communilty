from __future__ import annotations

import pytest

from aikernel.core.cycle import CognitiveCycleRunner, CycleConfig
from aikernel.core.models.cognitive import CycleStep
from aikernel.core.models.envelope import CommandEnvelope, ResultStatus


class TestCognitiveCycleRunner:
    @pytest.mark.asyncio
    async def test_Run_SimpleCommand_ShouldReturnResult(self):
        runner = CognitiveCycleRunner()
        result = await runner.run("hello")
        assert result is not None
        assert result.status == ResultStatus.SUCCESS

    @pytest.mark.asyncio
    async def test_Run_Output_ShouldContainProcessed(self):
        runner = CognitiveCycleRunner()
        result = await runner.run("test input")
        assert "Processed" in result.output

    @pytest.mark.asyncio
    async def test_Run_EmotionalDelta_ShouldBePresent(self):
        config = CycleConfig(enable_emotions=True)
        runner = CognitiveCycleRunner(config=config)
        result = await runner.run("hello")
        assert result.emotional_delta is not None

    @pytest.mark.asyncio
    async def test_Run_DisableEmotions_ShouldBeNone(self):
        config = CycleConfig(enable_emotions=False)
        runner = CognitiveCycleRunner(config=config)
        result = await runner.run("hello")
        assert result.emotional_delta is None

    @pytest.mark.asyncio
    async def test_Events_ShouldEmitAllSteps(self):
        runner = CognitiveCycleRunner()
        events = []

        def capture(event):
            events.append(event)

        runner.on_event(capture)
        await runner.run("test")
        assert len(events) > 0
        assert events[0].step == CycleStep.SENSOR

    @pytest.mark.asyncio
    async def test_Run_MultipleIterations_ShouldSucceed(self):
        config = CycleConfig(max_iterations=3)
        runner = CognitiveCycleRunner(config=config)
        result = await runner.run("test")
        assert result.status == ResultStatus.SUCCESS

    @pytest.mark.asyncio
    async def test_Stream_ShouldYieldEvents(self):
        runner = CognitiveCycleRunner()
        cmd = CommandEnvelope(payload="test")
        events = []
        async for event in runner.stream_cycle(cmd):
            events.append(event)
        assert len(events) > 0

    @pytest.mark.asyncio
    async def test_RunCommand_WithEnvelope_ShouldWork(self):
        runner = CognitiveCycleRunner()
        cmd = CommandEnvelope(payload="hello world")
        result = await runner.run_command(cmd)
        assert result.command_id == cmd.id

    @pytest.mark.asyncio
    async def test_Duration_ShouldBePositive(self):
        runner = CognitiveCycleRunner()
        result = await runner.run("test")
        assert result.duration_ms >= 0

    @pytest.mark.asyncio
    async def test_SafetyVerdict_ShouldBeEvaluated(self):
        runner = CognitiveCycleRunner()
        cmd = CommandEnvelope(payload="safe input")
        result = await runner.run_command(cmd)
        assert result.risk_score >= 0.0

    @pytest.mark.asyncio
    async def test_Memory_ShouldBeUsed(self):
        runner = CognitiveCycleRunner()
        await runner.run("first")
        await runner.run("second")
        assert runner.episodic_memory.count > 0
        assert runner.working_memory.count > 0
