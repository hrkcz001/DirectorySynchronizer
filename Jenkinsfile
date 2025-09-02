pipeline {
    agent none

    stages {
        stage('Load repository') {
            agent any
            steps {
                checkout scm
            }
        }

        stage('Test on Windows') {
            agent { label 'windows-agent' }
            steps {
                bat '''
                   cd DirectorySynchronizer.Tests
                   dotnet test
                '''
            }
        }

        stage('Test on Linux') {
            agent { label 'linux-agent' }
            steps {
                sh '''
                   cd DirectorySynchronizer.Tests
                   dotnet test
                '''
            }
        }
    }
}

