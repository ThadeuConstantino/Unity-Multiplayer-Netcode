#!/usr/bin/env bash
set -euf -o pipefail

GENERATOR_VERSION=v0.12.0
QOS_API_VERSION=6e87ace75135688460b5df3fb67b5c2c6bb504cc

function fail {
    echo "${1:-}"
    exit 1
}

function install_fetch {
    binary_name=""
    unameOut="$(uname -s)"
    case "${unameOut}" in
        Linux*)      binary_name=fetch_linux_amd64;;
        Darwin*)     binary_name=fetch_darwin_amd64;;
        MINGW64_NT*) binary_name=fetch_windows_amd64.exe;;
        *)          fail "unsupported os $unameOut"
    esac
    url=https://github.com/gruntwork-io/fetch/releases/download/v0.4.1/${binary_name}
    curl -sfL -o fetch ${url} || fail "Could not download fetch from ${url}"
    chmod +x fetch
}

function install_yq {
    binary_name=""
    unameOut="$(uname -s)"
    case "${unameOut}" in
        Linux*)       binary_name=yq_linux_amd64;;
        Darwin*)      binary_name=yq_darwin_amd64;;
        MINGW64_NT*)  binary_name=yq_windows_amd64.exe;;
        *)          fail "unsupported os $unameOut"
    esac
    url=https://github.com/mikefarah/yq/releases/download/v4.6.3/${binary_name}
    curl -sfL -o yq ${url} || fail "Could not download yq from ${url}"
    chmod +x yq
}

if [[ -z ${GITHUB_OAUTH_TOKEN} ]]; then
cat << EOT
GITHUB_OAUTH_TOKEN not set, it is needed to download generator.jar from github.

You can create a personal https://docs.github.com/en/github/authenticating-to-github/creating-a-personal-access-token. Or if running in GitHub Actions the token can be obtained from secrets.GITHUB_TOKEN.
The token must be set into the variable GITHUB_OAUTH_TOKEN and exported to the environment (e.g. export GITHUB_OAUTH_TOKEN=<token>).
EOT
    fail
fi

echo "Checking java installation..."
java -version || fail "java not found, make sure it is installed and in your \$PATH"


mkdir -p build

pushd build

# fetch is used to download artifacts from GitHub
# it uses the GitHub API under the hood
# a simple curl command against the releases URL wouldn't work due to authentication
# authentication token is expected to be set in GITHUB_OAUTH_TOKEN
install_fetch

# yq is used to do some light postprocessing the open api spec
install_yq

echo "Downloading code generator jar ${GENERATOR_VERSION}"
./fetch --repo="https://github.com/Unity-Technologies/openapi-generators" --tag=${GENERATOR_VERSION} --release-asset="generator.jar" . || fail "Could not download generator jar from github releases"

echo "Downloading OpenAPI spec ${QOS_API_VERSION}"
./fetch --repo="https://github.com/Unity-Technologies/mp-suite-qos-discovery-svc" --tag=${QOS_API_VERSION} --source-path="/api" mp-suite-qos-discovery-svc/api || fail "Could not download OpenAPI spec"

if [ ! -f mp-suite-qos-discovery-svc/api/qos.openapi.yaml ]; then
    fail "Cannot continue $0 because the OpenAPI spec is not in the expected location."
fi

# filter out X-Player and X-Request-Id header parameters from openapi spec
# we cannot remove all parameters since we need to keep the service & region params
# therefore we select using array indices, but this is not ideal as it could break
# if params are added/removed
# a better solution should be implemented
echo "Filtering open api spec file"
cat mp-suite-qos-discovery-svc/api/qos.openapi.yaml| ./yq e 'del(.paths[].get.parameters[3])' - | ./yq e 'del(.paths[].get.parameters[2])' - > qos.openapi.filtered.yaml || fail "Failed to filter openapi spec file"

echo "Adding servers to open api spec file"
cat << EOF >> qos.openapi.filtered.yaml
servers:
  - url: https://qos-discovery.services.api.unity.com
EOF

popd

echo "Running code generation"
java -jar build/generator.jar generate -c generator-config.yaml || fail "Failed to generate code"

# clean up
rm -rf build
