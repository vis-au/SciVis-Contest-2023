FROM python:3.10.5-slim as base


ENV VAR1=10

# Keeps Python from generating .pyc files in the container
ENV PYTHONDONTWRITEBYTECODE=1

# Turns off buffering for easier container logging
ENV PYTHONUNBUFFERED=1

FROM base AS python-deps

# Install & use pipenv
COPY Pipfile ./
RUN apt-get update && apt-get install -y --no-install-recommends build-essential libssl-dev gcc make cmake
RUN python -m pip install --upgrade pip
RUN pip install pipenv
RUN mkdir .venv
RUN pipenv install

FROM base AS runtime

# Copy virtual env
COPY --from=python-deps /.venv /.venv
ENV PATH="/.venv/bin:$PATH"

# Create user
RUN useradd --create-home -ms /bin/bash admin
WORKDIR /home/brain/
USER admin

COPY --chown=admin:admin . .
COPY --chown=admin:admin ./protos/ .

# Compile proto

RUN python -m grpc_tools.protoc -I./protos --python_out=./server/ --pyi_out=./server/ --grpc_python_out=./server/ ./protos/brain.proto

EXPOSE 50051
ENTRYPOINT ["python", "server/server.py"]
