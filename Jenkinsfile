pipeline {
  agent any
  stages {
    stage('build') {
      steps {
        sh 'dotnet build'
      }
    }
    stage('pack') {
      steps {
        sh 'dotnet pack'
      }
    }
  }
}