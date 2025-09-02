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
                   ls
                '''
            }
        }

        stage('Test on Linux') {
            agent { label 'linux-agent' }
            steps {
                sh '''
                   ls
                '''
            }
        }
    }
}

