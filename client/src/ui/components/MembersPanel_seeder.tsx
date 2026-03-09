export default function MembersPanel_seeder() {
    const members = ['Alex Rivera', 'Sarah Chen', 'Mike Ross']

    return (
        <aside className="hidden bg-white lg:block">
            <div className="border-b border-zinc-200 px-4 py-4">
                <h2 className="text-xs font-semibold uppercase tracking-[0.2em] text-zinc-400">
                    Members
                </h2>
            </div>

            <div className="space-y-4 px-4 py-4 text-sm">
                {members.map((member, index) => (
                    <div key={member} className="flex items-center gap-3">
                        <div
                            className={`h-8 w-8 rounded-full ${
                                index === 0 ? 'bg-orange-200' : index === 1 ? 'bg-teal-200' : 'bg-red-200'
                            }`}
                        />
                        <span>{member}</span>
                    </div>
                ))}
            </div>
        </aside>
    )
}