#pragma once
#include <memory>
#include <vector>
#include "../Core/Math/Vector.h"
namespace wakuwaku
{
	class Scene : public std::enable_shared_from_this<Scene>
	{
	public:
		using SharedPtr = std::shared_ptr<Scene>;

		SharedPtr static Create() { return std::shared_ptr<Scene>(new Scene()); };

		int AddMesh(float* positions, float* normals, float* tangents, float* uv1,int vertex_count,float* indices,int triangle_count)
		{
			//SubMeshDescriptor descriptor {
				//	.index_start = m_indices.size(), 
				//	.index_count = triangle_count * 3 ,
				//	.base_vertex = m_positions.size()};
				//
				//m_positions.reserve(m_positions.size() + vertex_count);
				//m_tangents.reserve(m_tangents.size() + vertex_count);
				//m_normals.reserve(m_normals.size() + vertex_count);
				//m_uv1s.reserve(m_uv1s.size() + vertex_count);
				//for (size_t i = 0; i < vertex_count; i++)
				//{
				//	m_positions.push_back({ positions[3 * i] ,positions[3 * i + 1] ,positions[3 * i + 2] });
				//	m_tangents.push_back({ positions[3 * i] ,positions[3 * i + 1] ,positions[3 * i + 2] });
				//	m_normals.push_back({ positions[3 * i] ,positions[3 * i + 1] ,positions[3 * i + 2] });
				//	m_uv1s.push_back({ positions[2 * i] ,positions[2 * i + 1]});
				//}
				//
				//m_indices.reserve(m_indices.size() + triangle_count * 3);
				//for (size_t i = 0; i < triangle_count * 3; i++)
				//{
				//	m_indices.push_back(indices[i]);
				//}

			int id = m_sub_mesh_descriptors.size();
			//m_sub_mesh_descriptors.push_back(descriptor);
			return id;
		}

		int AddInstance(float* world_matrix, int mesh_id, void* base_color);
		void ApplyCamera(float* position);

		void BuildScene();
	private:

	protected:
		struct SubMeshDescriptor
		{
			int index_start;
			int index_count;
			int base_vertex;
			//int first_vertex;
			//int vertex_count;
		};

		std::vector<SubMeshDescriptor> m_sub_mesh_descriptors;
		//std::unordered_map<SubMeshDescriptor,int>
		
		std::vector<DirectX::XMFLOAT3> m_positions;
		std::vector<DirectX::XMFLOAT3> m_tangents;
		std::vector<DirectX::XMFLOAT3> m_normals;
		std::vector<DirectX::XMFLOAT2> m_uv1s;
		std::vector<int> m_indices;

		struct Instance
		{
			DirectX::XMFLOAT4X4 local_to_world;
			int mesh_id;
		};
	private:
		Scene() = default;
	};
}