'use client'

import { useState } from 'react'
import {
  Activity,
  AlertTriangle,
  Bell,
  ChevronDown,
  CircleHelp,
  Fan,
  Gauge,
  LayoutDashboard,
  Menu,
  MoreHorizontal,
  Power,
  Refrigerator,
  Search,
  Settings2,
  ShieldCheck,
  Snowflake,
  Thermometer,
  Utensils,
  Waves,
  X,
  Zap,
} from 'lucide-react'

const equipment = [
  { id: 'walk-in-freezer-1', name: 'Walk-in freezer 1', zone: 'Cold storage', value: '-18', unit: '°C', icon: Snowflake, status: 'Normal', accent: 'blue', detail: 'Compressor on · 860 W', controls: ['ON', 'OFF'] },
  { id: 'walk-in-freezer-2', name: 'Walk-in freezer 2', zone: 'Cold storage', value: '-17', unit: '°C', icon: Snowflake, status: 'Normal', accent: 'blue', detail: 'Compressor on · 820 W', controls: ['ON', 'OFF'] },
  { id: 'stove-1', name: 'Stove 1', zone: 'Line 01', value: '190', unit: '°C', icon: Utensils, status: 'Running', accent: 'orange', detail: '2 burners heating · 2.4 kW', controls: ['ON', 'OFF'] },
  { id: 'stove-2', name: 'Stove 2', zone: 'Line 02', value: '175', unit: '°C', icon: Utensils, status: 'Running', accent: 'orange', detail: '1 burner heating · 1.6 kW', controls: ['ON', 'OFF'] },
  { id: 'oven-1', name: 'Oven 1', zone: 'Line 01', value: '180', unit: '°C', icon: Gauge, status: 'Running', accent: 'yellow', detail: 'Bake cycle · 3.2 kW', controls: ['ON', 'OFF'] },
  { id: 'oven-2', name: 'Oven 2', zone: 'Line 02', value: '165', unit: '°C', icon: Gauge, status: 'Normal', accent: 'yellow', detail: 'Standby · 120 W', controls: ['ON', 'OFF'] },
  { id: 'mixer-1', name: 'Mixer 1', zone: 'Prep station', value: '120', unit: 'RPM', icon: Activity, status: 'Running', accent: 'purple', detail: 'Mixing mode · 500 W', controls: ['ON', 'OFF'] },
  { id: 'mixer-2', name: 'Mixer 2', zone: 'Prep station', value: '0', unit: 'RPM', icon: Activity, status: 'Normal', accent: 'purple', detail: 'Idle mode · 0 W', controls: ['ON', 'OFF'] },
  { id: 'exhaust-fan-1', name: 'Exhaust fan 1', zone: 'Ventilation', value: '60', unit: '%', icon: Fan, status: 'Running', accent: 'green', detail: 'Airflow nominal · 1,200 CFM', controls: ['ON', 'OFF'] },
  { id: 'exhaust-fan-2', name: 'Exhaust fan 2', zone: 'Ventilation', value: '45', unit: '%', icon: Fan, status: 'Running', accent: 'green', detail: 'Airflow nominal · 900 CFM', controls: ['ON', 'OFF'] },
  { id: 'blender-1', name: 'Blender 1', zone: 'Prep station', value: '8,000', unit: 'RPM', icon: Zap, status: 'Normal', accent: 'cyan', detail: 'Idle mode · 0 W', controls: ['ON', 'OFF'] },
  { id: 'blender-2', name: 'Blender 2', zone: 'Prep station', value: '6,500', unit: 'RPM', icon: Zap, status: 'Normal', accent: 'cyan', detail: 'Idle mode · 0 W', controls: ['ON', 'OFF'] },
  { id: 'dishwasher-1', name: 'Dishwasher 1', zone: 'Back of house', value: '60', unit: '°C', icon: Waves, status: 'Running', accent: 'purple', detail: 'Washing cycle · 1.0 kW', controls: ['START', 'STOP'] },
  { id: 'dishwasher-2', name: 'Dishwasher 2', zone: 'Back of house', value: '42', unit: '°C', icon: Waves, status: 'Normal', accent: 'purple', detail: 'Standby · 80 W', controls: ['START', 'STOP'] },
  { id: 'sink-1', name: 'Sink 1', zone: 'Back of house', value: '30', unit: '°C', icon: Waves, status: 'Normal', accent: 'cyan', detail: 'Valve open · 4 L/min', controls: ['OPEN', 'CLOSED'] },
  { id: 'sink-2', name: 'Sink 2', zone: 'Back of house', value: '29', unit: '°C', icon: Waves, status: 'Normal', accent: 'cyan', detail: 'Valve closed · 0 L/min', controls: ['OPEN', 'CLOSED'] },
]

const accentStyles: Record<string, { color: string; soft: string; line: string }> = {
  cyan: { color: '#2dd4bf', soft: 'rgba(45,212,191,.12)', line: '#2dd4bf' },
  orange: { color: '#fb923c', soft: 'rgba(251,146,60,.12)', line: '#fb923c' },
  blue: { color: '#60a5fa', soft: 'rgba(96,165,250,.12)', line: '#60a5fa' },
  yellow: { color: '#facc15', soft: 'rgba(250,204,21,.12)', line: '#facc15' },
  purple: { color: '#c084fc', soft: 'rgba(192,132,252,.12)', line: '#c084fc' },
  green: { color: '#4ade80', soft: 'rgba(74,222,128,.12)', line: '#4ade80' },
}

function StatusDot({ status }: { status: string }) {
  const isRunning = status === 'Running'
  return <span className={`status-dot ${isRunning ? 'running' : ''}`} aria-label={status} />
}

function EquipmentCard({ item, isOn, onToggle }: { item: (typeof equipment)[number]; isOn: boolean; onToggle: () => void }) {
  const Icon = item.icon
  const accent = accentStyles[item.accent]
  return (
    <article className="equipment-card" style={{ '--accent': accent.color, '--accent-soft': accent.soft } as React.CSSProperties}>
      <div className="equipment-card-top">
        <div className="equipment-icon"><Icon size={18} strokeWidth={1.8} /></div>
        <div className="equipment-heading">
          <h3>{item.name}</h3>
          <p>{item.zone}</p>
        </div>
        <button className="icon-button" aria-label={`More options for ${item.name}`}><MoreHorizontal size={18} /></button>
      </div>
      <div className="reading">
        <span className="reading-value">{item.value}</span><span className="reading-unit">{item.unit}</span>
      </div>
      <div className="equipment-status"><StatusDot status={item.status} /><span>{item.status}</span><span className="status-detail">{item.detail}</span></div>
      <div className="card-footer">
        <div className="mini-sparkline" aria-hidden="true"><span /><span /><span /><span /><span /><span /><span /></div>
        <div className="control-group">
          {item.controls.map((control) => <button key={control} onClick={onToggle} className={`control-button ${control === (isOn ? item.controls[0] : item.controls[1]) ? 'selected' : ''}`}>{control}</button>)}
        </div>
      </div>
    </article>
  )
}

function TrendChart() {
  return (
    <div className="trend-chart" aria-label="Temperature trend chart showing oven, fryer and refrigerator readings">
      <div className="chart-y-labels"><span>200°</span><span>150°</span><span>100°</span><span>50°</span><span>0°</span></div>
      <svg viewBox="0 0 700 190" preserveAspectRatio="none" role="img" aria-hidden="true">
        <defs><linearGradient id="oven-fill" x1="0" x2="0" y1="0" y2="1"><stop offset="0" stopColor="#fb923c" stopOpacity=".24" /><stop offset="1" stopColor="#fb923c" stopOpacity="0" /></linearGradient></defs>
        <g className="chart-grid"><line x1="0" y1="10" x2="700" y2="10" /><line x1="0" y1="52" x2="700" y2="52" /><line x1="0" y1="94" x2="700" y2="94" /><line x1="0" y1="136" x2="700" y2="136" /><line x1="0" y1="178" x2="700" y2="178" /></g>
        <path d="M0 168 C52 165 55 162 95 164 S144 152 180 125 S212 82 257 69 S310 51 350 53 S398 45 433 39 S485 41 522 34 S570 32 605 28 S660 30 700 23 L700 190 L0 190 Z" fill="url(#oven-fill)" />
        <path className="chart-line orange-line" d="M0 168 C52 165 55 162 95 164 S144 152 180 125 S212 82 257 69 S310 51 350 53 S398 45 433 39 S485 41 522 34 S570 32 605 28 S660 30 700 23" />
        <path className="chart-line cyan-line" d="M0 174 C50 173 88 172 130 173 S200 171 250 172 S315 171 365 172 S430 169 480 170 S545 167 600 169 S655 166 700 166" />
        <path className="chart-line blue-line" d="M0 178 C60 178 90 179 145 178 S220 179 270 178 S340 179 390 178 S465 177 520 178 S610 177 700 178" />
      </svg>
      <div className="chart-x-labels"><span>10:00</span><span>11:00</span><span>12:00</span><span>13:00</span><span>14:00</span><span>Now</span></div>
    </div>
  )
}

export default function Page() {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [onStates, setOnStates] = useState<Record<string, boolean>>({
    'walk-in-freezer-1': true, 'walk-in-freezer-2': true,
    'stove-1': true, 'stove-2': true,
    'oven-1': true, 'oven-2': false,
    'mixer-1': true, 'mixer-2': false,
    'exhaust-fan-1': true, 'exhaust-fan-2': true,
    'blender-1': false, 'blender-2': false,
    'dishwasher-1': true, 'dishwasher-2': false,
    'sink-1': true, 'sink-2': true,
  })
  const [alarmOpen, setAlarmOpen] = useState(true)
  const toggle = (id: string) => setOnStates((current) => ({ ...current, [id]: !current[id] }))

  return (
    <main className="scada-shell">
      <aside className={`sidebar ${isMenuOpen ? 'open' : ''}`}>
        <div className="brand"><img className="brand-logo" src="https://hebbkx1anhila5yf.public.blob.vercel-storage.com/image-ZpaDIbT7xarKdw5AElI7mOf53ZKzrL.png" alt="KitchenOS" /><button className="mobile-close" onClick={() => setIsMenuOpen(false)} aria-label="Close menu"><X size={20} /></button></div>
        <nav className="side-nav" aria-label="Primary navigation"><p className="nav-label">WORKSPACE</p><a className="active" href="#overview"><LayoutDashboard size={17} /> Overview <span className="nav-count">16</span></a><a href="#alarms"><Bell size={17} /> Alarms <span className="nav-alert">2</span></a><a href="#analytics"><Activity size={17} /> Analytics</a><p className="nav-label">SYSTEM</p><a href="#equipment"><Settings2 size={17} /> Equipment</a><a href="#settings"><ShieldCheck size={17} /> Safety & access</a></nav>
        <div className="sidebar-bottom"><div className="facility-card"><div className="facility-avatar">KF</div><div><strong>Kitchen Floor</strong><span>North campus · Building A</span></div><ChevronDown size={15} /></div><div className="user-row"><div className="user-avatar">JD</div><div><strong>Jordan Davis</strong><span>Facility manager</span></div><MoreHorizontal size={17} /></div></div>
      </aside>
      {isMenuOpen && <button className="mobile-scrim" onClick={() => setIsMenuOpen(false)} aria-label="Close navigation" />}
      <section className="workspace">
        <header className="topbar"><button className="menu-button" onClick={() => setIsMenuOpen(true)} aria-label="Open navigation"><Menu size={21} /></button><div className="breadcrumb"><span>Kitchen Floor</span><span>/</span><strong>Overview</strong></div><div className="top-actions"><div className="live-indicator"><i /> Live data <span>·</span> 14:45:32</div><button className="icon-button"><Search size={18} /></button><button className="icon-button notification"><Bell size={18} /><b>2</b></button><div className="top-avatar">JD</div></div></header>
        <div className="content">
          <div className="page-heading"><div><p className="eyebrow">MONDAY, SEPTEMBER 25, 2026 <span>·</span> SHIFT A</p><h1>Good afternoon, Jordan.</h1><p className="subheading">Here&apos;s the latest from your kitchen operations.</p></div><button className="outline-button"><Activity size={16} /> Generate report</button></div>
          <section className="overview-strip" aria-label="System overview"><div className="system-health"><div className="health-icon"><ShieldCheck size={21} /></div><div><span>System status</span><strong>All systems normal</strong></div></div><div className="overview-stat"><span>Online equipment</span><strong>12 <small>/ 16</small></strong></div><div className="overview-stat"><span>Active alarms</span><strong className="alarm-number">2</strong></div><div className="overview-stat"><span>Energy today</span><strong>42.8 <small>kWh</small></strong></div><div className="overview-updated"><span>Last sync</span><strong>Just now</strong></div></section>
          <section id="equipment" className="section-block"><div className="section-heading"><div><h2>Equipment status</h2><p>Real-time readings from connected kitchen assets</p></div><button className="filter-button">All equipment <ChevronDown size={15} /></button></div><div className="equipment-grid">{equipment.map((item) => <EquipmentCard key={item.id} item={item} isOn={onStates[item.id]} onToggle={() => toggle(item.id)} />)}</div></section>
          <div className="lower-grid"><section id="alarms" className="panel alarm-panel"><div className="panel-heading"><div><h2>Active alarms <span className="heading-badge">2</span></h2><p>Needs attention</p></div><button className="icon-button"><MoreHorizontal size={18} /></button></div>{alarmOpen ? <div className="alarm-list"><div className="alarm-item critical"><div className="alarm-icon"><AlertTriangle size={17} /></div><div><strong>Freezer temperature high</strong><p>Walk-in freezer · Reading is -8°C</p><span>Triggered 2 minutes ago</span></div><button onClick={() => setAlarmOpen(false)} className="ack-button">Acknowledge</button></div><div className="alarm-item warning"><div className="alarm-icon"><AlertTriangle size={17} /></div><div><strong>Refrigerator door open</strong><p>Cold storage · Door has been open</p><span>Triggered 2 minutes ago</span></div><button onClick={() => setAlarmOpen(false)} className="ack-button">Acknowledge</button></div></div> : <div className="empty-state"><ShieldCheck size={22} /> All alarms acknowledged</div>}</section><section id="analytics" className="panel trend-panel"><div className="panel-heading"><div><h2>Temperature trend</h2><p>Last 5 hours across monitored assets</p></div><button className="filter-button">Last 5 hours <ChevronDown size={15} /></button></div><div className="legend"><span><i className="legend-dot orange" /> Oven</span><span><i className="legend-dot cyan" /> Refrigerator</span><span><i className="legend-dot blue" /> Freezer</span></div><TrendChart /></section></div>
          <footer className="footer"><span><CircleHelp size={14} /> Need help? Contact support</span><span>KitchenOS v2.4.1 <i /> Secure connection</span></footer>
        </div>
      </section>
    </main>
  )
}
