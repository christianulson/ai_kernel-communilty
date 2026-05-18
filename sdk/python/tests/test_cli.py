from __future__ import annotations

from unittest.mock import patch

from krnlai.cli.commands.deploy import _generate_docker, _generate_kubernetes
from krnlai.cli.commands.init import TEMPLATE_AGENT_PY, TEMPLATE_ENV, init_project
from krnlai.cli.commands.security import _audit_test_cases, _run_benchmark
from krnlai.core.safety.rules import SafetyChecker


class TestSecurityCommands:
    def test_AuditTestCases_ShouldHave10(self):
        cases = _audit_test_cases()
        assert len(cases) == 10

    def test_Audit_ShouldRunWithoutError(self):
        checker = SafetyChecker()
        for name, ctx in _audit_test_cases():
            verdict = checker.evaluate_all(ctx)
            assert hasattr(verdict, "allowed")
            assert hasattr(verdict, "blocked_by")

    def test_Benchmark_ShouldMeasure(self):
        with patch("rich.console.Console.print") as mock:
            _run_benchmark(["100"])
            assert mock.called


class TestDeployCommands:
    def test_GenerateDocker_ShouldCreateFiles(self):
        import os
        import tempfile
        orig_dir = os.getcwd()
        with tempfile.TemporaryDirectory() as tmp:
            os.chdir(tmp)
            _generate_docker("test-agent")
            assert os.path.exists("Dockerfile")
            assert os.path.exists("docker-compose.yml")
            with open("Dockerfile") as f:
                content = f.read()
                assert "python:3.12-slim" in content
            os.chdir(orig_dir)

    def test_GenerateKubernetes_ShouldCreateManifest(self):
        import os
        import tempfile
        orig_dir = os.getcwd()
        with tempfile.TemporaryDirectory() as tmp:
            os.chdir(tmp)
            _generate_kubernetes("test-agent")
            assert os.path.exists("k8s/deployment.yaml")
            os.chdir(orig_dir)


class TestInitCommand:
    def test_Templates_ShouldHaveContent(self):
        assert "CognitiveAgent" in TEMPLATE_AGENT_PY
        assert "OPENAI_API_KEY" in TEMPLATE_ENV

    def test_InitProject_ShouldCreateFiles(self):
        import os
        import tempfile
        orig_dir = os.getcwd()
        with tempfile.TemporaryDirectory() as tmp:
            os.chdir(tmp)
            init_project("my-agent")
            assert os.path.exists("my-agent/agent.py")
            assert os.path.exists("my-agent/.env")
            assert os.path.exists("my-agent/requirements.txt")
            os.chdir(orig_dir)
