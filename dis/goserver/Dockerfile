# syntax=docker/dockerfile:1

FROM golang:1.20-alpine

RUN apk add --no-cache protoc

WORKDIR /app

COPY go.mod ./
COPY go.sum ./
RUN go mod tidy
RUN go get -u google.golang.org/protobuf/proto
RUN go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
RUN go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
RUN go mod tidy
RUN go mod download

RUN ls /go/bin
RUN export PATH="$PATH:$(go env GOPATH)/bin"

COPY . .


RUN protoc --go_out=. --go_opt=paths=source_relative \
	--go-grpc_out=. --go-grpc_opt=paths=source_relative \
	./brain/*.proto


RUN go build -o /docker-brain

EXPOSE 50052

CMD [ "/docker-brain" ]