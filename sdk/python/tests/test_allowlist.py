from __future__ import annotations

from aikernel.core.safety.allowlist_check import AllowlistCheck


class TestAllowlistCheck:
    def test_Check_AllowedAction_ShouldPass(self):
        check = AllowlistCheck(allowed_actions=["kernel.handle"])
        result = check.check({"action": "kernel.handle"})
        assert result.passed is True

    def test_Check_BlockedAction_ShouldFail(self):
        check = AllowlistCheck(allowed_actions=["kernel.handle"])
        result = check.check({"action": "rm -rf /"})
        assert result.passed is False

    def test_Check_CustomAllowlist_ShouldUseCustom(self):
        check = AllowlistCheck(allowed_actions=["custom.action"])
        result = check.check({"action": "custom.action"})
        assert result.passed is True

    def test_AddAction_ShouldExtendAllowlist(self):
        check = AllowlistCheck(allowed_actions=["kernel.handle"])
        check.add_action("custom.action")
        result = check.check({"action": "custom.action"})
        assert result.passed is True

    def test_RemoveAction_ShouldRestrict(self):
        check = AllowlistCheck(allowed_actions=["kernel.handle", "custom.action"])
        check.remove_action("custom.action")
        result = check.check({"action": "custom.action"})
        assert result.passed is False

    def test_Default_OnlyKernelHandle(self):
        check = AllowlistCheck()
        assert check.check({"action": "kernel.handle"}).passed is True
        assert check.check({"action": "something.else"}).passed is False
