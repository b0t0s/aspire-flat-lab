FROM quay.io/jupyter/scipy-notebook:latest

USER root

# Lua interpreter for the ILua kernel (xeus-lua has no arm64 build on conda-forge).
RUN apt-get update \
    && apt-get install -y --no-install-recommends lua5.4 \
    && ln -sf /usr/bin/lua5.4 /usr/local/bin/lua \
    && rm -rf /var/lib/apt/lists/*

# .NET Interactive: C#, F# and PowerShell kernels. Microsoft deprecated it in March 2026; 1.0.712001 is its last release.
# Kernels go to /opt/conda, because /home/jovyan is a bind mount that would hide anything installed there.
ENV DOTNET_ROOT=/opt/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_ROLL_FORWARD=Major \
    PATH=/opt/dotnet:/opt/dotnet-tools:$PATH
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet \
    && rm /tmp/dotnet-install.sh \
    && dotnet tool install --tool-path /opt/dotnet-tools Microsoft.dotnet-interactive --version 1.0.712001 \
    && dotnet-interactive jupyter install --path /opt/conda/share/jupyter/kernels \
    && fix-permissions /opt/conda

COPY latex.json /etc/jupyter/jupyter_server_config.d/latex.json

USER ${NB_UID}

# LaTeX documents (Tectonic fetches TeX packages on demand), Markdown files as notebooks, Lua kernel.
RUN mamba install -y -c conda-forge tectonic jupyterlab-latex jupytext \
    && pip install --no-cache-dir ilua \
    && mamba clean -afy \
    && fix-permissions /opt/conda
