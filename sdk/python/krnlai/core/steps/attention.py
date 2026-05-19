from __future__ import annotations

import re
from typing import Any, Dict

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class EnhancedAttentionStep:
    INTENT_PATTERNS: dict[str, list[str]] = {
        "question": ["?", "what", "why", "how", "when", "where", "who", "which"],
        "command": ["run", "execute", "do", "create", "make", "start", "stop"],
        "statement": [],
        "request": ["please", "could you", "can you", "would you", "i need", "i want"],
        "analysis": ["analyze", "compare", "evaluate", "assess", "review", "examine"],
        "creative": ["write", "compose", "design", "imagine", "create", "brainstorm", "generate"],
        "social": ["hello", "hi", "thanks", "thank", "how are you"],
        "metacognitive": ["think", "reason", "consider", "reflect", "ponder", "wonder"],
        "memory_recall": ["remember", "recall", "what did", "previous", "before", "earlier"],
        "instruction": ["follow", "step", "instruction", "guide", "manual", "procedure"],
    }

    THOUGHT_STRUCTURE_PATTERNS: dict[str, list[str]] = {
        "analytical": ["analyze", "compare", "contrast", "evaluate", "assess", "break down"],
        "creative": ["imagine", "create", "design", "invent", "compose", "innovate"],
        "critical": ["critique", "criticize", "flaw", "weakness", "limitation", "problem"],
        "factual_recall": ["what is", "define", "explain", "describe", "what was"],
        "procedural": ["how to", "steps", "instructions", "process", "method", "procedure"],
        "emotional": ["feel", "feeling", "emotion", "angry", "happy", "sad", "love", "hate"],
        "social": ["hello", "hi", "thanks", "apologize", "congratulate", "introduce"],
        "metacognitive": ["i think", "i believe", "i consider", "i wonder", "i reflect"],
    }

    URGENCY_KEYWORDS = {
        "urgent": 0.9, "asap": 0.9, "immediately": 0.9, "now": 0.8,
        "critical": 0.9, "emergency": 1.0, "deadline": 0.7, "due": 0.6,
        "soon": 0.5, "important": 0.6, "priority": 0.7, "quick": 0.5,
    }

    COMPLEXITY_DOMAIN_KEYWORDS = {
        "code": 2, "algorithm": 3, "mathematics": 3, "analysis": 2,
        "system": 2, "architecture": 3, "science": 2, "philosophy": 3,
    }

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        payload = cmd.payload

        intent = self._classify_intent(payload)
        topic = self._extract_topic(payload)
        complexity = self._assess_complexity(payload)
        urgency = self._assess_urgency(payload)
        thought_type = self._classify_thought_structure(payload)
        category = self._map_to_thought_category(intent, thought_type)

        return {
            "intent": intent,
            "topic": topic,
            "complexity": complexity,
            "urgency": urgency,
            "thought_type": thought_type,
            "category": category,
            "requires_decomposition": complexity > 0.7,
        }

    @staticmethod
    def _classify_intent(text: str) -> str:
        text_lower = text.lower()
        best_intent = "statement"
        best_score = 0

        for intent, patterns in EnhancedAttentionStep.INTENT_PATTERNS.items():
            if not patterns:
                continue
            score = sum(1 for p in patterns if p in text_lower)
            if score > best_score:
                best_score = score
                best_intent = intent

        return best_intent

    @staticmethod
    def _extract_topic(text: str) -> str:
        stop_words = {"the", "a", "an", "is", "are", "was", "were", "be", "been",
                      "being", "have", "has", "had", "do", "does", "did", "will",
                      "would", "could", "should", "may", "might", "shall", "can",
                      "to", "of", "in", "for", "on", "with", "at", "by", "from",
                      "as", "into", "through", "during", "before", "after", "about",
                      "between", "under", "over", "and", "or", "but", "not", "this",
                      "that", "these", "those", "it", "its", "i", "you", "he", "she",
                      "we", "they", "me", "him", "her", "us", "them", "my", "your",
                      "his", "their", "what", "which", "who", "how", "when", "where"}

        words = re.findall(r'\b[a-zA-Z]{3,}\b', text.lower())
        content_words = [w for w in words if w not in stop_words]
        word_freq: dict[str, int] = {}
        for w in content_words:
            word_freq[w] = word_freq.get(w, 0) + 1

        sorted_words = sorted(word_freq.items(), key=lambda x: -x[1])
        if not sorted_words:
            return "general"
        return sorted_words[0][0]

    @staticmethod
    def _assess_complexity(text: str) -> float:
        words = text.split()
        word_count = len(words)
        if word_count == 0:
            return 0.0

        text_lower = text.lower()
        sentence_count = len(re.findall(r'[.!?]+', text))
        avg_sentence_length = word_count / max(1, sentence_count)

        length_score = min(1.0, word_count / 300.0)
        sentence_score = min(1.0, avg_sentence_length / 30.0)

        domain_score = 0.0
        for keyword, weight in EnhancedAttentionStep.COMPLEXITY_DOMAIN_KEYWORDS.items():
            if keyword in text_lower:
                domain_score = max(domain_score, weight / 3.0)

        conditional_score = 0.0
        conditionals = ["if", "then", "else", "unless", "provided", "although", "however"]
        conditional_count = sum(1 for c in conditionals if c in text_lower)
        conditional_score = min(1.0, conditional_count * 0.15)

        score = length_score * 0.3 + sentence_score * 0.25 + domain_score * 0.25 + conditional_score * 0.2
        return round(min(1.0, max(0.0, score)), 2)

    @staticmethod
    def _assess_urgency(text: str) -> float:
        text_lower = text.lower()
        max_urgency = 0.0
        for keyword, score in EnhancedAttentionStep.URGENCY_KEYWORDS.items():
            if keyword in text_lower:
                max_urgency = max(max_urgency, score)
        return max_urgency

    @staticmethod
    def _classify_thought_structure(text: str) -> str:
        text_lower = text.lower()
        best_type = "analytical"
        best_score = 0

        for thought_type, patterns in EnhancedAttentionStep.THOUGHT_STRUCTURE_PATTERNS.items():
            score = sum(1 for p in patterns if p in text_lower)
            if score > best_score:
                best_score = score
                best_type = thought_type

        return best_type

    @staticmethod
    def _map_to_thought_category(intent: str, thought_type: str) -> str:
        mapping: dict[str, str] = {
            "question": "pattern_match",
            "command": "communication",
            "statement": "context_aware",
            "request": "social",
            "analysis": "evaluative",
            "creative": "analogy_formation",
            "social": "theory_of_mind",
            "metacognitive": "self_observation",
            "memory_recall": "episodic_recall",
            "instruction": "sequence_planning",
        }
        return mapping.get(intent, "context_aware")
