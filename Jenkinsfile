pipeline {
    agent any

    stages {
        stage('Load repository') {
            steps {
                checkout scm
            }
        }

        stage('Test on Windows Docker Container') {
            steps {
                bat '''
                     echo %WORKSPACE%
                     docker run --rm ^
                      -v %WORKSPACE%:/workspace ^
                      -w /workspace ^
                      mcr.microsoft.com/dotnet/sdk:9.0-nanoserver-ltsc2025 ^
                      dotnet test
                '''
            }
        }

        /*stage('Test on Linux Docker Container') {
            steps {
                sh '''
                    cd /workspace/DirectorySynchronizer.Tests
                    dotnet add reference ../DirectorySynchronizer/DirectorySynchronizer.csproj
                    dotnet test
                '''
            }
        }*/
    }
}