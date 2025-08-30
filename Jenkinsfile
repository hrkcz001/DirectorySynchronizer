pipeline {
    agent any

    stages {
        stage('Load repository') {
            steps {
                checkout scm
            }
        }

        stage('Test on Windows Docker Container') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:9.0-nanoserver-ltsc2025'
                    args '-v ${WORKSPACE}:/workspace'
                }
            }
            steps {
                bat '''
                    cd /workspace/DirectorySynchronizer.Tests
                    dotnet add reference ../DirectorySynchronizer/DirectorySynchronizer.csproj
                    dotnet test
                '''
            }
        }

        stage('Test on Linux Docker Container') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:9.0-noble-amd64'
                    args '-v ${WORKSPACE}:/workspace'
                }
            }
            steps {
                sh '''
                    cd /workspace/DirectorySynchronizer.Tests
                    dotnet add reference ../DirectorySynchronizer/DirectorySynchronizer.csproj
                    dotnet test
                '''
            }
        }
    }
}