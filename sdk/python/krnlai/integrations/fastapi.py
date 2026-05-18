from __future__ import annotations

from typing import Any, Callable, Optional

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

from krnlai.core.cycle import CognitiveCycleRunner


class AikernelMiddleware(BaseHTTPMiddleware):
    def __init__(
        self,
        app: Any,
        runner: Optional[CognitiveCycleRunner] = None,
        exclude_paths: Optional[list] = None,
    ) -> None:
        super().__init__(app)
        self._runner = runner or CognitiveCycleRunner()
        self._exclude_paths = exclude_paths or ["/health", "/docs", "/openapi.json"]

    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        if request.url.path in self._exclude_paths:
            return await call_next(request)

        body = await request.body()
        if body:
            result = await self._runner.run(body.decode("utf-8", errors="ignore"))
            if not result.safety_verdict or not getattr(result.safety_verdict, "allowed", True):
                from starlette.responses import JSONResponse
                return JSONResponse(
                    status_code=403,
                    content={
                        "error": "Blocked by AI Kernel safety system",
                        "risk_score": result.risk_score,
                        "reason": result.error,
                    },
                )

        response = await call_next(request)
        return response
