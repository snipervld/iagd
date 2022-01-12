#include "GrimTypes.h"

namespace GAME {
	std::wstring itemReplicaToString(GAME::ItemReplicaInfo replica) {
		std::wstringstream stream;
		stream << replica.baseRecord.c_str() << ";";
		stream << replica.prefixRecord.c_str() << ";";
		stream << replica.suffixRecord.c_str() << ";";
		stream << replica.seed << ";";
		stream << replica.modifierRecord.c_str() << ";";
		stream << replica.materiaRecord.c_str() << ";";
		stream << replica.relicBonus.c_str() << ";";
		stream << replica.relicSeed << ";";
		stream << replica.enchantmentRecord.c_str() << ";";
		stream << replica.enchantmentSeed << ";";
		stream << replica.transmuteRecord.c_str();

		return stream.str();
	}

	/// <summary>
	/// Helper method for converting gameTextLine to a CSV string.
	/// </summary>
	/// <param name="gameTextLines"></param>
	/// <returns></returns>
	std::wstring gameTextLineToString(std::vector<GameTextLine>& gameTextLines) {
		std::wstringstream stream;
		GAME::ItemReplicaInfo replica;

		for (auto& it : gameTextLines) {
			stream << it.textClass << ";" << it.text.c_str() << "\n";
		}

		std::wstring str = stream.str();
		return str;
	}



}
GAME::GameEngine* fnGetgGameEngine()
{
	return (GAME::GameEngine*)*(DWORD_PTR*)GetProcAddress(GetModuleHandle(L"game.dll"), "?gGameEngine@GAME@@3PEAVGameEngine@1@EA");
}