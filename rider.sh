#!/bin/bash

# ! Linux version !
DOCKER_GROUP_ID=$(cut -d: -f3 < <(getent group docker))
USER_ID=$(id -u $(whoami))
GROUP_ID=$(id -g $(whoami))

cmd="docker run \
--env HOME=${HOME} \
--env DISPLAY=unix${DISPLAY} \
--interactive \
--net host \
--tty \
--group-add ${DOCKER_GROUP_ID} \
--user=${USER_ID}:${GROUP_ID} \
--volume ${HOME}:${HOME} \
--volume=\"/etc/group:/etc/group:ro\" \
--volume=\"/etc/passwd:/etc/passwd:ro\" \
--volume=\"/etc/shadow:/etc/shadow:ro\" \
--volume /var/run/docker.sock:/var/run/docker.sock \
--rm \
--workdir ${HOME}/ \
--entrypoint \"/bin/bash\" \
lean-rider \
-c \"cd ${HOME}; /opt/JetBrains\ Rider-2020.3.2/bin/rider.sh\""

echo $cmd

eval "$cmd"

