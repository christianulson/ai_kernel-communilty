from __future__ import annotations

from dataclasses import dataclass, field
from typing import List
from uuid import UUID, uuid4


@dataclass
class PainRewardSignal:
    id: UUID = field(default_factory=uuid4)
    value: float = 0.0
    is_pain: bool = False
    label: str = ""

    @classmethod
    def reward(cls, value: float, label: str = "") -> PainRewardSignal:
        return cls(value=value, is_pain=False, label=label or f"reward:{value}")

    @classmethod
    def pain(cls, value: float, label: str = "") -> PainRewardSignal:
        return cls(value=value, is_pain=True, label=label or f"pain:{value}")


class PainRewardModel:
    def __init__(self, learning_rate: float = 0.1) -> None:
        self._learning_rate = learning_rate
        self._accumulated_pain: float = 0.0
        self._accumulated_reward: float = 0.0
        self._history: List[PainRewardSignal] = []

    def apply(self, signal: PainRewardSignal) -> None:
        if signal.is_pain:
            self._accumulated_pain += signal.value * self._learning_rate
        else:
            self._accumulated_reward += signal.value * self._learning_rate
        self._history.append(signal)

    def apply_many(self, signals: List[PainRewardSignal]) -> None:
        for signal in signals:
            self.apply(signal)

    @property
    def net_hedonic_value(self) -> float:
        return self._accumulated_reward - self._accumulated_pain

    @property
    def pain_level(self) -> float:
        return self._accumulated_pain

    @property
    def reward_level(self) -> float:
        return self._accumulated_reward

    @property
    def history(self) -> List[PainRewardSignal]:
        return list(self._history)

    def reset(self) -> None:
        self._accumulated_pain = 0.0
        self._accumulated_reward = 0.0
        self._history.clear()
