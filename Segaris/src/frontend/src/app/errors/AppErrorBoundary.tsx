import { Component, type ErrorInfo, type ReactNode } from 'react'
import { RefreshCw } from 'lucide-react'
import { withTranslation, type WithTranslation } from 'react-i18next'

import { Button } from '@/components/ui'

interface Props extends WithTranslation {
  children: ReactNode
  level?: 'root' | 'module'
  onReturnToLauncher?: () => void
}

interface State {
  error: Error | null
}

class AppErrorBoundaryBase extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Rendering failure', { error, componentStack: info.componentStack })
  }

  reset = () => this.setState({ error: null })

  render() {
    if (this.state.error === null) return this.props.children

    const { t, level = 'module', onReturnToLauncher } = this.props
    return (
      <main className="seg-system-screen armali-aurora">
        <section className="seg-state-card" role="alert">
          <h1>
            {t(level === 'root' ? 'errors.rootRenderTitle' : 'errors.renderTitle')}
          </h1>
          <p>{t(level === 'root' ? 'errors.rootRenderBody' : 'errors.renderBody')}</p>
          <Button
            iconLeft={<RefreshCw size={17} />}
            onClick={level === 'root' ? () => window.location.reload() : this.reset}
          >
            {t(level === 'root' ? 'common.reload' : 'common.tryAgain')}
          </Button>
          {level === 'module' && onReturnToLauncher != null && (
            <Button variant="outline" onClick={onReturnToLauncher}>
              {t('common.returnToLauncher')}
            </Button>
          )}
        </section>
      </main>
    )
  }
}

export const AppErrorBoundary = withTranslation('platform')(AppErrorBoundaryBase)
